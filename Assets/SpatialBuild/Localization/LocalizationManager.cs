using System;
using System.Collections.Generic;
using System.Linq;
using SpatialBuild.Coordinates;
namespace SpatialBuild.Localization {
 public enum LocalizationState { UNINITIALIZED,CALIBRATING,VALID,DEGRADED,INVALID,LOST }
 [Serializable] public sealed class ControlReference {
  public string nodeId,datumDescription,calibrationVersion,family;
  public int markerId;public double markerSizeM=.2;
  public RigidD projectFromDatum=RigidD.Identity,datumFromMarker=RigidD.Identity;
  public bool surveyed,physicalValidationPassed,orientationCalibrated;
  public RigidD ProjectFromMarker=>projectFromDatum*datumFromMarker;
 }
 [Serializable] public sealed class PoseMeasurement {
  public string source,nodeId,calibrationVersion;
  public double captureSeconds,positionSigmaM,orientationSigmaRad,quality;
  public RigidD questFromMarker;
  public bool valid,simulated;
 }
 public interface ILocalizationSource {bool TryDequeue(out PoseMeasurement measurement);}
 [Serializable] public sealed class LocalizationConfig {
  public double maximumAgeSeconds=.5,calibrationWindowSeconds=20,lostAfterSeconds=5,maxResidualM=.03,maxAngularResidualRad=.04,maxCorrectionM=.15,correctionTimeSeconds=.5;
  public int minimumReferences=3;
 }
 public sealed class LocalizationManager {
  public readonly LocalizationConfig config;
  readonly Dictionary<string,ControlReference> controls;
  readonly Dictionary<string,PoseMeasurement> observations=new Dictionary<string,PoseMeasurement>();
  public LocalizationState State {get;private set;}=LocalizationState.UNINITIALIZED;
  public string Reason {get;private set;}="Select project and measured control";
  public RigidD QuestFromProject {get;private set;}=RigidD.Identity;
  public DVec ProjectOrigin {get;private set;}
  public RigidD QuestFromProjectLocal {get;private set;}=RigidD.Identity;
  RigidD targetLocal=RigidD.Identity;bool initialized;
  public double LastCorrection {get;private set;}=double.NaN;
  public double RmseM {get;private set;}
  public double MaxResidualM {get;private set;}
  public readonly Dictionary<string,double> residuals=new Dictionary<string,double>();
  public event Action<string> Changed;
  public LocalizationManager(IEnumerable<ControlReference> nodes,LocalizationConfig settings=null){controls=nodes.ToDictionary(n=>n.nodeId);config=settings??new LocalizationConfig();ProjectOrigin=controls.Count>0?controls.Values.First().ProjectFromMarker.position:new DVec();}
  public void Begin(){observations.Clear();residuals.Clear();initialized=false;LastCorrection=double.NaN;RmseM=0;MaxResidualM=0;State=LocalizationState.CALIBRATING;Reason="Searching for at least three surveyed references";Changed?.Invoke(Reason);}
  public void Invalidate(string reason){State=LocalizationState.INVALID;Reason=reason;Changed?.Invoke(reason);}
  public void TrackingLost(){State=LocalizationState.LOST;initialized=false;observations.Clear();Reason="Quest tracking lost; recalibrate";Changed?.Invoke(Reason);}
  public bool Submit(PoseMeasurement m,double now){
   if(State==LocalizationState.INVALID||State==LocalizationState.LOST||State==LocalizationState.UNINITIALIZED)return false;
   if(m==null||!m.valid||m.simulated||m.source!="FIDUCIAL_CAMERA"||!DVec.Number(m.captureSeconds)||now-m.captureSeconds>config.maximumAgeSeconds||now<m.captureSeconds||!m.questFromMarker.position.Finite||m.quality<=0||!DVec.Number(m.quality)||m.positionSigmaM<=0||!DVec.Number(m.positionSigmaM)||m.orientationSigmaRad<=0||!DVec.Number(m.orientationSigmaRad))return false;
   if(!controls.TryGetValue(m.nodeId,out var node)||!node.surveyed||node.calibrationVersion!=m.calibrationVersion)return false;
   if(observations.TryGetValue(m.nodeId,out var old)&&m.captureSeconds<=old.captureSeconds)return false;
   observations[m.nodeId]=m;
   // Quest tracking relates camera poses observed at different times. A site
   // worker can scan three dispersed markers in sequence, within this window.
   var current=observations.Values.Where(o=>now-o.captureSeconds<=config.calibrationWindowSeconds).ToArray();
   if(current.Length<config.minimumReferences)return false;
   // Reject degenerate control geometry. Do not fit one unknown point repeatedly.
   double area=0;for(int i=1;i<current.Length;i++)for(int j=i+1;j<current.Length;j++)area=Math.Max(area,DVec.Cross(controls[current[i].nodeId].ProjectFromMarker.position-controls[current[0].nodeId].ProjectFromMarker.position,controls[current[j].nodeId].ProjectFromMarker.position-controls[current[0].nodeId].ProjectFromMarker.position).Norm);
   if(area<.01){Reason="Control geometry is nearly collinear";return false;}
   var localFromProject=new RigidD(ProjectOrigin,DQuat.Identity);
   var projectPoints=current.Select(o=>controls[o.nodeId].ProjectFromMarker.position).ToArray();
   var questPoints=current.Select(o=>o.questFromMarker.position).ToArray();
   var weights=current.Select(o=>1/(o.positionSigmaM*o.positionSigmaM)).ToArray();
   if(!RigidFit.TrySolve(projectPoints,questPoints,weights,ProjectOrigin,out var estimate)){Reason="Rigid control fit failed";return false;}
   double squared=0;MaxResidualM=0;residuals.Clear();bool failed=false;
   for(int i=0;i<current.Length;i++){
    var predicted=estimate*(localFromProject.Inverse*controls[current[i].nodeId].ProjectFromMarker);double residual=(predicted.position-current[i].questFromMarker.position).Norm;
    residuals[current[i].nodeId]=residual;squared+=residual*residual;MaxResidualM=Math.Max(MaxResidualM,residual);
    failed|=residual>config.maxResidualM||(controls[current[i].nodeId].orientationCalibrated&&DQuat.Angle(predicted.rotation,current[i].questFromMarker.rotation)>config.maxAngularResidualRad);
   }
   RmseM=Math.Sqrt(squared/current.Length);
   if(failed){Invalidate("Trusted reference disagreement; all precision guidance invalid");return false;}
   if(initialized&&(estimate.position-QuestFromProjectLocal.position).Norm>config.maxCorrectionM){Invalidate("Correction exceeds configured gate; recalibration required");return false;}
   targetLocal=estimate;if(!initialized){QuestFromProjectLocal=estimate;QuestFromProject=estimate*localFromProject.Inverse;initialized=true;}
   LastCorrection=now;State=current.All(o=>controls[o.nodeId].physicalValidationPassed)?LocalizationState.VALID:LocalizationState.DEGRADED;
   Reason=State==LocalizationState.VALID?"Reference fit accepted; inspect independent accuracy log":"Reference fit only; physical validation pending";
   Changed?.Invoke(Reason);return true;
  }
  public void Tick(double now,double dt){
   if(!initialized||State==LocalizationState.INVALID||State==LocalizationState.LOST)return;
   if(now-LastCorrection>config.lostAfterSeconds){State=LocalizationState.LOST;Reason="Absolute references stale";Changed?.Invoke(Reason);return;}
   if(now-LastCorrection>config.maximumAgeSeconds){State=LocalizationState.DEGRADED;Reason="Awaiting fresh absolute control";}
   double f=1-Math.Exp(-Math.Max(0,dt)/Math.Max(.001,config.correctionTimeSeconds));QuestFromProjectLocal=RigidD.Blend(QuestFromProjectLocal,targetLocal,f);QuestFromProject=QuestFromProjectLocal*new RigidD(ProjectOrigin,DQuat.Identity).Inverse;
  }
 }
}
