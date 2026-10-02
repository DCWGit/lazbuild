using System;
using System.Collections.Generic;
using System.Linq;
using AprilTag;
using Meta.XR;
using SpatialBuild.Coordinates;
using UnityEngine;

namespace SpatialBuild.Localization {
 // Camera observations are a real sensor source. They are never marked as
 // surveyed or physically validated by the detector itself.
 public sealed class FiducialCameraSource : MonoBehaviour,ILocalizationSource {
  public PassthroughCameraAccess cameraAccess;
  public string Status {get;private set;}="Camera not started";
  readonly Queue<PoseMeasurement> pending=new Queue<PoseMeasurement>();
  readonly Dictionary<double,TagDetector> detectors=new Dictionary<double,TagDetector>();
  readonly Dictionary<int,List<ControlReference>> tags=new Dictionary<int,List<ControlReference>>();
  double lastProcess;
  Vector2Int resolution;
  public void Configure(IEnumerable<ControlReference> controls){
   tags.Clear();foreach(var c in controls){if(!tags.TryGetValue(c.markerId,out var list))tags[c.markerId]=list=new List<ControlReference>();list.Add(c);}
   foreach(var detector in detectors.Values)detector.Dispose();detectors.Clear();pending.Clear();resolution=default;
  }
  public void StartCapture(){
   if(tags.Count==0){Status="No measured control profile";return;}
   if(!OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.PassthroughCameraAccess)){
    OVRPermissionsRequester.Request(new[]{OVRPermissionsRequester.Permission.PassthroughCameraAccess});
    Status="Waiting for headset camera permission";
   }
   if(!cameraAccess)cameraAccess=gameObject.AddComponent<PassthroughCameraAccess>();
   cameraAccess.CameraPosition=PassthroughCameraAccess.CameraPositionType.Left;
   cameraAccess.enabled=true;Status="Waiting for left camera frames";
  }
  public void StopCapture(){if(cameraAccess)cameraAccess.enabled=false;foreach(var d in detectors.Values)d.Dispose();detectors.Clear();pending.Clear();Status="Camera stopped";}
  public bool TryDequeue(out PoseMeasurement measurement){if(pending.Count>0){measurement=pending.Dequeue();return true;}measurement=null;return false;}
  void Update(){
   if(!cameraAccess||!cameraAccess.IsPlaying||!cameraAccess.IsUpdatedThisFrame||Time.realtimeSinceStartupAsDouble-lastProcess<.2)return;
   lastProcess=Time.realtimeSinceStartupAsDouble;
   var current=cameraAccess.CurrentResolution;var intrinsics=cameraAccess.Intrinsics;
   if(current.x<=0||current.y<=0||intrinsics.SensorResolution.x<=0||intrinsics.SensorResolution.y<=0){Status="Camera intrinsics unavailable";return;}
   double age=(DateTime.UtcNow-cameraAccess.Timestamp.ToUniversalTime()).TotalSeconds;
   if(age<0||age>.5){Status="Camera frame stale or clock inconsistent";return;}
   var cameraPose=cameraAccess.GetCameraPose();
   if(cameraPose.rotation.x==0&&cameraPose.rotation.y==0&&cameraPose.rotation.z==0&&cameraPose.rotation.w==0){Status="Capture-time camera pose unavailable";return;}
   var pixels=cameraAccess.GetColors();if(!pixels.IsCreated||pixels.Length!=current.x*current.y){Status="Camera pixels unavailable";return;}
   if(resolution!=current){foreach(var d in detectors.Values)d.Dispose();detectors.Clear();resolution=current;}
   // Mirror MRUK's CalcSensorCropRegion; GetColors is bottom-up, the vendored
   // AprilTag converter flips it to its top-down native image.
   var k=CameraIntrinsicsMath.Native(intrinsics.SensorResolution.x,intrinsics.SensorResolution.y,current.x,current.y,intrinsics.FocalLength.x,intrinsics.FocalLength.y,intrinsics.PrincipalPoint.x,intrinsics.PrincipalPoint.y);
   if(!k.Valid){Status="Invalid camera calibration";return;}
   var image=pixels.ToArray();var questFromCamera=UnityFrames.FromUnity(cameraPose);
   int found=0;double capture=Time.realtimeSinceStartupAsDouble-age;
   foreach(var size in tags.Values.SelectMany(v=>v).Select(c=>c.markerSizeM).Distinct()){
    if(size<=0||size>2)continue;
    if(!detectors.TryGetValue(size,out var detector))detectors[size]=detector=new TagDetector(current.x,current.y);
    detector.ProcessImage(image,k.fx,k.fy,k.cx,k.cy,(float)size);
    foreach(var pose in detector.DetectedTags){
     if(!tags.TryGetValue(pose.ID,out var refs))continue;
     foreach(var control in refs){if(control.markerSizeM!=size)continue;
      var cameraFromMarker=UnityFrames.FromUnity(new Pose(pose.Position,pose.Rotation));
      pending.Enqueue(new PoseMeasurement{source="FIDUCIAL_CAMERA",nodeId=control.nodeId,calibrationVersion=control.calibrationVersion,captureSeconds=capture,questFromMarker=questFromCamera*cameraFromMarker,positionSigmaM=.02,orientationSigmaRad=.03,quality=1,valid=true,simulated=false});found++;
     }
    }
   }
   Status=found>0?"Observed "+found+" configured markers":"Searching for configured markers";
  }
  void OnDestroy(){StopCapture();}
 }
}
