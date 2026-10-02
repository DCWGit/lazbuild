using System;
using System.Linq;
using SpatialBuild.Coordinates;
using SpatialBuild.Localization;

static class Program {
 static void Near(DVec actual,DVec expected,double tolerance,string test){if((actual-expected).Norm>tolerance)throw new Exception(test+" error "+(actual-expected).Norm);}
 static void Check(bool value,string test){if(!value)throw new Exception(test);}
 static RigidD Pose(double x,double y,double z,double degrees){double a=degrees*Math.PI/360;return new RigidD(new DVec(x,y,z),new DQuat(0,0,Math.Sin(a),Math.Cos(a)));}
 static PoseMeasurement Observe(ControlReference c,RigidD questFromProject,double time,DVec offset){return new PoseMeasurement {source="FIDUCIAL_CAMERA",nodeId=c.nodeId,calibrationVersion=c.calibrationVersion,captureSeconds=time,positionSigmaM=.01,orientationSigmaRad=.01,quality=1,valid=true,simulated=false,questFromMarker=new RigidD(questFromProject.Apply(c.ProjectFromMarker.position)+offset,questFromProject.rotation*c.ProjectFromMarker.rotation)};}
 static ControlReference Control(string id,DVec p){return new ControlReference {nodeId=id,calibrationVersion="v1",family="tagStandard41h12",markerId=int.Parse(id),markerSizeM=.2,surveyed=true,projectFromDatum=new RigidD(p,DQuat.Identity),datumFromMarker=RigidD.Identity};}
 static void Main(){
  var q=Pose(2,-3,4,39);var p=new DVec(1.2,-.8,7);Near(q.Inverse.Apply(q.Apply(p)),p,1e-12,"rigid inverse");
  var b=Pose(-4,2,1,-17);Near((q*b).Apply(p),q.Apply(b.Apply(p)),1e-12,"composition order");
  var chain=new CoordinateChain {ProjectFromBim=Pose(12,4,-3,15),QuestFromProject=q,ProjectOrigin=new DVec(1000000,2000000,3000000),QuestFromHead=Pose(1,2,3,90)};
  Near(chain.QuestToProject(chain.BimToQuest(p)),chain.ProjectFromBim.Apply(p),1e-11,"BIM project Quest chain");
  Near(chain.QuestFromProjectLocal.Apply(new DVec(0,0,0)),chain.QuestFromProject.Apply(chain.ProjectOrigin),1e-9,"local origin");
  Near((chain.QuestFromProject*chain.ProjectFromHead).position,chain.QuestFromHead.position,1e-12,"head transform");
  var matrix=new double[]{0,-1,0,1000000,1,0,0,2000000,0,0,1,3000000,0,0,0,1};
  Near(MatrixFrames.FromRowMajor(matrix).Apply(new DVec(2,3,4)),new DVec(999997,2000002,3000004),1e-9,"BIM project matrix");
  matrix[0]=2;bool scaleRejected=false;try{MatrixFrames.FromRowMajor(matrix);}catch(ArgumentException){scaleRejected=true;}Check(scaleRejected,"unmeasured scale rejected");
  Check(Math.Abs(new DVec(0,0,1000).Norm-1000)<1e-12,"metres preserved");
  var o=new DVec(1000000,2000000,3000000);
  var cs=new[]{Control("1",o),Control("2",o+new DVec(5,0,0)),Control("3",o+new DVec(0,5,0))};
  var actual=Pose(-200,75,1.4,23);var manager=new LocalizationManager(cs);manager.Begin();
  Check(!manager.Submit(Observe(cs[0],actual,1,new DVec()),1.1),"one control not sufficient");
  Check(!manager.Submit(Observe(cs[1],actual,2,new DVec()),2.1),"two controls not sufficient");
  var differentlyMounted=Observe(cs[2],actual,3,new DVec());differentlyMounted.questFromMarker=new RigidD(differentlyMounted.questFromMarker.position,Pose(0,0,0,105).rotation);
  Check(manager.Submit(differentlyMounted,3.1),"three differently oriented controls fit from centers");
  Check(manager.State==LocalizationState.DEGRADED,"unvalidated controls not green");
  Near(manager.QuestFromProject.Apply(o),actual.Apply(o),1e-8,"million metre origin alignment");
  Check(manager.RmseM<1e-8,"zero synthetic residual");
  var sim=Observe(cs[0],actual,3.5,new DVec());sim.simulated=true;Check(!manager.Submit(sim,3.6),"simulated measurement rejected");
  var stale=Observe(cs[0],actual,3,new DVec());Check(!manager.Submit(stale,4),"old capture rejected");
  var moved=Observe(cs[2],actual,4,new DVec(.1,0,0));Check(!manager.Submit(moved,4.1),"moved trusted reference rejected");
  Check(manager.State==LocalizationState.INVALID,"reference disagreement latches invalid");
  Check(!manager.Submit(Observe(cs[0],actual,4.2,new DVec()),4.3),"invalid cannot silently recover");
  manager.Begin();Check(manager.State==LocalizationState.CALIBRATING,"explicit reset");
  var collinear=new[]{Control("1",o),Control("2",o+new DVec(1,0,0)),Control("3",o+new DVec(2,0,0))};
  var degenerate=new LocalizationManager(collinear);degenerate.Begin();foreach(var c in collinear)degenerate.Submit(Observe(c,actual,1,new DVec()),1.1);
  Check(degenerate.State==LocalizationState.CALIBRATING,"collinear controls refused");
  var k=CameraIntrinsicsMath.Native(1280,960,640,480,900,890,700,430);
  Check(Math.Abs(k.fx-450)<1e-9&&Math.Abs(k.fy-445)<1e-9&&Math.Abs(k.cx-350)<1e-9&&Math.Abs(k.cy-265)<1e-9,"scaled principal point and y flip");
  var crop=CameraIntrinsicsMath.Native(1280,960,1280,720,900,890,640,480);
  Check(Math.Abs(crop.cx-640)<1e-9&&Math.Abs(crop.cy-360)<1e-9,"sensor crop");
  Console.WriteLine("PASS coordinate chain, large origin, differently mounted targets, source gating, reference disagreement, latch, degeneracy, camera crop");
 }
}
