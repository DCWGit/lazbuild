using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpatialBuild.Bim;
using SpatialBuild.Coordinates;
using SpatialBuild.Localization;
using UnityEngine;
using UnityEngine.XR;

namespace SpatialBuild.Quest {
 // Startup path for field use. The existing manual miniature stays behind an
 // explicit developer action and does not certify BIM registration.
 public sealed class FieldProjectController : MonoBehaviour {
  public QuestRegistrationController developerDemo;
  public Transform eye,trackingSpace;
  public OVRHand rightHand;
  CurvedPreviewPanel panel;BimModelView model;FiducialCameraSource source;
  LocalizationManager localization;ControlProfile profile;
  readonly PinchGate pinch=new PinchGate();
  readonly List<XRInputSubsystem> subsystems=new List<XRInputSubsystem>();
  LineRenderer pointer;Material pointerMaterial;GameObject dot;TextMesh statusText;
  bool projectLoaded,inspection,developer,scanning,headWasTracked;
  bool displaySubscribed,originsSubscribed;
  int tab,filterIndex;float clickUntil;double lastLog;
  Vector3 lastTrackingPosition;Quaternion lastTrackingRotation;
  readonly string[] filters={"ALL","ARCHITECTURAL","STRUCTURAL","MECHANICAL","ELECTRICAL","PLUMBING","FIRE_PROTECTION","OTHER"};
  string status="Select a project with MENU";
  string session=Guid.NewGuid().ToString("N");
  void Awake(){
   if(developerDemo){developerDemo.enabled=false;if(developerDemo.handControls)developerDemo.handControls.enabled=false;if(developerDemo.world)developerDemo.world.gameObject.SetActive(false);}
  }
  void Start(){
   if(!eye||!trackingSpace){enabled=false;Debug.LogError("SpatialBuild field rig missing");return;}
   lastTrackingPosition=trackingSpace.position;lastTrackingRotation=trackingSpace.rotation;
   var p=new GameObject("Field menu");p.transform.SetParent(eye,false);panel=p.AddComponent<CurvedPreviewPanel>();panel.Build();
   var line=new GameObject("Field pointer");line.transform.SetParent(transform,false);pointer=line.AddComponent<LineRenderer>();pointer.positionCount=2;pointer.startWidth=.003f;pointer.endWidth=.002f;
   pointerMaterial=new Material(Shader.Find("SpatialBuild/TransparentGuidance"));pointerMaterial.color=Color.cyan;pointer.sharedMaterial=pointerMaterial;
   dot=GameObject.CreatePrimitive(PrimitiveType.Sphere);dot.name="Field pinch pointer";dot.transform.SetParent(transform,false);dot.transform.localScale=Vector3.one*.014f;Destroy(dot.GetComponent<Collider>());dot.GetComponent<Renderer>().sharedMaterial=pointerMaterial;
   statusText=CurvedPreviewPanel.Text(eye,"SPATIALBUILD\nMENU: SELECT PROJECT",new Vector3(0,.31f,1.18f),.004f);
   statusText.color=new Color(1,.73f,.3f);source=gameObject.AddComponent<FiducialCameraSource>();
   model=new GameObject("IFC view").AddComponent<BimModelView>();
   OVRManager.TrackingLost+=TrackingLost;OVRManager.HMDUnmounted+=TrackingLost;
   SubscribeTrackingEvents();
   Log("session_start","Field startup; no surveyed control",null);
  }
  void TrackingLost(){localization?.TrackingLost();if(model){model.SetTrust(false,false);model.SetVisible(false);}inspection=false;status="Tracking lost. Recalibrate.";Log("tracking_lost",status,null);}
  void OriginChanged(XRInputSubsystem subsystem)=>TrackingLost();
  void SubscribeTrackingEvents(){
   if(!displaySubscribed&&OVRManager.display!=null){OVRManager.display.RecenteredPose+=TrackingLost;displaySubscribed=true;}
   if(!originsSubscribed){SubsystemManager.GetSubsystems(subsystems);foreach(var s in subsystems)s.trackingOriginUpdated+=OriginChanged;originsSubscribed=subsystems.Count>0;}
  }
  void Update(){
   if(developer)return;
   SubscribeTrackingEvents();
   bool tracked=false;var head=InputDevices.GetDeviceAtXRNode(XRNode.Head);
   if(head.TryGetFeatureValue(CommonUsages.isTracked,out bool h)&&h&&head.TryGetFeatureValue(CommonUsages.trackingState,out InputTrackingState ht))tracked=(ht&(InputTrackingState.Position|InputTrackingState.Rotation))==(InputTrackingState.Position|InputTrackingState.Rotation);
   if(headWasTracked&&!tracked)TrackingLost();headWasTracked=tracked;
   if((trackingSpace.position-lastTrackingPosition).sqrMagnitude>.000001f||Quaternion.Angle(trackingSpace.rotation,lastTrackingRotation)>.01f){if(localization!=null)TrackingLost();lastTrackingPosition=trackingSpace.position;lastTrackingRotation=trackingSpace.rotation;}
   if(localization!=null){
    while(source.TryDequeue(out var measurement)){
     double now=Time.realtimeSinceStartupAsDouble;bool accepted=localization.Submit(measurement,now);
     Log(accepted?"fiducial_accepted":"fiducial_rejected",measurement.nodeId,measurement);
    }
    localization.Tick(Time.realtimeSinceStartupAsDouble,Time.unscaledDeltaTime);
    if(localization.State==LocalizationState.VALID||localization.State==LocalizationState.DEGRADED||localization.State==LocalizationState.INVALID){
     if(!inspection&&model.Project!=null){model.SetProjectPose(localization.QuestFromProject);model.SetVisible(true);model.SetTrust(localization.State==LocalizationState.VALID,false);}
    }
    if(localization.State==LocalizationState.LOST){model.SetVisible(false);}
    if(Time.realtimeSinceStartupAsDouble-lastLog>2){Log("localization_state",localization.State+" "+localization.Reason,null);lastLog=Time.realtimeSinceStartupAsDouble;}
   }
   bool valid=rightHand&&rightHand.IsTracked&&rightHand.IsDataHighConfidence&&rightHand.IsPointerPoseValid&&!rightHand.IsSystemGestureInProgress&&rightHand.GetFingerConfidence(OVRHand.HandFinger.Index)==OVRHand.TrackingConfidence.High;
   bool pinching=valid&&rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index),pressed=pinch.Pressed(valid,pinching);
   if(!valid){pointer.enabled=false;dot.SetActive(false);panel.Collapse();Present(-1,false);return;}
   var ray=new Ray(rightHand.PointerPose.position,rightHand.PointerPose.forward);int hovered=panel.Hover(ray,Time.unscaledDeltaTime,out float distance);
   if(pressed)clickUntil=Time.unscaledTime+.18f;bool purple=pinching||Time.unscaledTime<clickUntil;
   pointerMaterial.color=purple?new Color(.78f,.25f,1,1):Color.cyan;pointer.enabled=true;pointer.SetPosition(0,ray.origin);pointer.SetPosition(1,ray.GetPoint(distance));dot.SetActive(true);dot.transform.position=ray.GetPoint(distance);dot.transform.localScale=Vector3.one*(purple?.023f:.014f);
   if(pressed){if(hovered>=0)Execute(hovered);else if(model.Project!=null)status=model.Select(ray);}
   Present(hovered,purple);
  }
  void LoadProject(){
   source.StopCapture();localization=null;profile=null;scanning=false;
   if(!model.Load("DentalClinic/project")){status="Project import missing";return;}
   projectLoaded=true;inspection=false;model.SetVisible(false);
   var asset=Resources.Load<TextAsset>("DentalClinic/control-profile");profile=asset?JsonUtility.FromJson<ControlProfile>(asset.text):null;
   if(profile==null||profile.schemaVersion!="spatialbuild.control.v1"||profile.projectId!=model.Project.projectId||profile.projectRevision!=model.Project.revision||profile.controls==null){status="IFC loaded. Control profile absent or revision mismatch.";return;}
   if(profile.controls.Any(c=>c.calibrationVersion!=profile.calibrationVersion||c.family!="tagStandard41h12"||c.markerSizeM<=0)) {status="IFC loaded. Control profile has invalid family, size, or version.";return;}
   localization=new LocalizationManager(profile.controls);source.Configure(profile.controls);
   status=profile.controls.Length>=3?"IFC loaded. Scan measured controls to register.":"IFC loaded. Three surveyed targets are needed.";
   Log("project_loaded",model.Project.projectId+" revision "+model.Project.revision,null);
  }
  void Inspect(){
   if(!projectLoaded){status="Select project first";return;}
   inspection=true;model.SetTrust(false,true);model.SetVisible(true);
   var bounds=model.Project.sources.Where(s=>s.boundsMin!=null&&s.boundsMax!=null).ToArray();
   double[] mid=new double[3];for(int axis=0;axis<3;axis++)mid[axis]=(bounds.Min(s=>s.boundsMin[axis])+bounds.Max(s=>s.boundsMax[axis]))/2-model.Project.projectOrigin[axis];
   var localCenter=UnityFrames.ToUnity(new DVec(mid[0],mid[1],mid[2]));
   var root=model.ModelRoot;root.localScale=Vector3.one*.02f;root.rotation=Quaternion.Euler(0,eye.eulerAngles.y,0);
   root.position=eye.position+eye.forward*2f-eye.up*.15f-root.rotation*(localCenter*.02f);
   status="BLUE MODEL PREVIEW | 1:50 | NOT REGISTERED";
   Log("inspection_preview",status,null);
  }
  void Scan(){
   if(localization==null||profile.controls.Length<3){status="Add three measured controls to control-profile.json";return;}
   inspection=false;model.SetVisible(false);model.ModelRoot.localScale=Vector3.one;
   localization.Begin();source.StartCapture();scanning=true;status="Scanning measured tagStandard41h12 controls";
   Log("calibration_begin",profile.calibrationVersion,null);
  }
  public void ReturnFromDeveloper(){
   if(!developer)return;
   developerDemo.Invalidate("Return to field mode");developerDemo.enabled=false;developerDemo.handControls.Suspend();developerDemo.handControls.enabled=false;developerDemo.world.gameObject.SetActive(false);
   developer=false;localization?.TrackingLost();model.SetVisible(false);inspection=false;scanning=false;pinch.Reset();panel.gameObject.SetActive(true);statusText.gameObject.SetActive(true);
   status="Field mode. Recalibrate measured controls.";Log("developer_demo_closed",status,null);
  }
  void Execute(int action){
   if(action==10){panel.ToggleWheel();return;}
   if(action<4){tab=action;panel.OpenTab();return;}
   int a=action-4;
   switch(tab){
    case 0:if(a==0)LoadProject();else if(a==1)Inspect();else if(a==2){inspection=false;model.SetVisible(false);status="Model hidden";}else if(a==3)status=model.Project==null?"No project":model.Project.projectName+" | "+model.Project.elements.Length+" IFC elements";break;
    case 1:if(a==0){filterIndex=(filterIndex+1)%filters.Length;model.SetFilter(filters[filterIndex]);status="Discipline: "+filters[filterIndex];if(model.Project!=null&&!model.Project.elements.Any(e=>e.geometryAvailable&&e.discipline==filters[filterIndex]))status=filters[filterIndex]+" has no renderable IFC shapes";}else if(a==1){filterIndex=0;model.SetFilter("ALL");status="All disciplines";}
     else if(a==2||a==3){model.SetElevation(true,model.ElevationM+(a==2?-.25:.25),model.HalfRangeM);status="Elevation "+model.ElevationM.ToString("F2")+" m +/- "+model.HalfRangeM.ToString("F2");}
     else if(a==4){model.SetElevation(!model.ElevationEnabled,model.ElevationM,model.HalfRangeM);status="Elevation slice "+(model.ElevationEnabled?"ON":"OFF");}
     else if(a==5){model.SetElevation(false,0,.5);status="All elevations";}break;
    case 2:if(a==0)Scan();else if(a==1){source.StopCapture();scanning=false;model.SetVisible(false);status="Scan stopped";}else if(a==2)status=source.Status;else if(a==3)status=localization==null?"No measured controls":localization.State+" | "+localization.Reason;break;
    case 3:if(a==0){developer=true;source.StopCapture();model.SetVisible(false);panel.gameObject.SetActive(false);statusText.gameObject.SetActive(false);pointer.enabled=false;dot.SetActive(false);developerDemo.world.gameObject.SetActive(true);developerDemo.enabled=true;developerDemo.handControls.enabled=true;Log("developer_demo_open","Manual preview",null);}break;
   }
  }
  void Present(int hovered,bool purple){
   string[] actions=tab==0?new[]{"LOAD\nDental Clinic","INSPECT\nblue 1:50","HIDE\nmodel","PROJECT\ninfo","", ""}:
    tab==1?new[]{"NEXT\ndiscipline","SHOW\nall","LOWER\n0.25 m","HIGHER\n0.25 m","SLICE\non/off","ALL\nelevations"}:
    tab==2?new[]{"START\nscan","STOP\nscan","CAMERA\nstatus","TRUST\nstatus","",""}:
    new[]{"OPEN\ndemo","","","","",""};
   string[] labels={"PROJECT","LAYERS","CONTROL","DEV",actions[0],actions[1],actions[2],actions[3],actions[4],actions[5]};
   string trust=localization==null?"UNINITIALIZED":localization.State.ToString();
   string message=projectLoaded?model.Project.projectName+" | "+trust:"No project selected";
   if(inspection)message="BLUE PREVIEW | NOT REGISTERED";
   if(localization!=null&&localization.State==LocalizationState.INVALID)message="RED | CONTROL DISAGREEMENT | STOP LAYOUT";
   statusText.text="SPATIALBUILD  "+trust+"\n"+message;
   statusText.color=trust=="VALID"?Color.green:trust=="INVALID"?Color.red:new Color(1,.73f,.3f);
   panel.Present(labels,tab,hovered,purple,status+"\n"+(scanning?source.Status:"Point and pinch | purple = input"));
  }
  void Log(string action,string details,PoseMeasurement measurement){try{
   var entry=new FieldEvent {utc=DateTime.UtcNow.ToString("O"),session=session,softwareVersion=Application.version,unityVersion=Application.unityVersion,deviceModel=SystemInfo.deviceModel,projectRevision=model&&model.Project!=null?model.Project.revision:"",calibrationVersion=profile!=null?profile.calibrationVersion:"",action=action,details=details,source=measurement?.source,nodeId=measurement?.nodeId,captureSeconds=measurement?.captureSeconds??-1,state=localization==null?"UNINITIALIZED":localization.State.ToString(),rmseM=localization?.RmseM??-1,maxResidualM=localization?.MaxResidualM??-1,observedQuestPosition=measurement==null?null:Values(measurement.questFromMarker.position),projectOrigin=localization==null?null:Values(localization.ProjectOrigin),questFromProjectPosition=localization==null?null:Values(localization.QuestFromProject.position),residuals=localization==null?"":string.Join(";",localization.residuals.Select(p=>p.Key+":"+p.Value.ToString("G17")))};
   File.AppendAllText(Path.Combine(Application.persistentDataPath,"field-"+session+".jsonl"),JsonUtility.ToJson(entry)+"\n");
  }catch(Exception e){Debug.LogWarning("SpatialBuild log: "+e.Message);}}
  static double[] Values(DVec v)=>new[]{v.x,v.y,v.z};
  [Serializable] sealed class FieldEvent {public string utc,session,softwareVersion,unityVersion,deviceModel,projectRevision,calibrationVersion,action,details,source,nodeId,state,residuals;public double captureSeconds,rmseM,maxResidualM;public double[] observedQuestPosition,projectOrigin,questFromProjectPosition;}
  void OnDestroy(){OVRManager.TrackingLost-=TrackingLost;OVRManager.HMDUnmounted-=TrackingLost;if(displaySubscribed&&OVRManager.display!=null)OVRManager.display.RecenteredPose-=TrackingLost;foreach(var s in subsystems)s.trackingOriginUpdated-=OriginChanged;if(pointerMaterial)Destroy(pointerMaterial);if(model)Destroy(model.gameObject);}
 }
}
