using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.XR;
using SpatialBuild.Positioning;
using SpatialBuild.Construction;

namespace SpatialBuild.Quest {
    // This is a manual controller-probe demonstration, never a survey instrument.
    public sealed class QuestRegistrationController : MonoBehaviour {
        public ConstructionWorld world;
        public Transform trackingSpace,eye;
        public Vector3 probeOffsetInControllerSpace;
        public Vector3[] controls={new Vector3(0,0,0),new Vector3(2,0,0),new Vector3(0,0,2)};
        public Vector3 independentCheck=new Vector3(2,0,2);
        public float maxFitM=.03f,maxCheckM=.03f;
        public string status="Capture controls to begin. Not registered.";
        public bool aligned{get;private set;}
        readonly List<Vector3> captured=new List<Vector3>();
        TextMesh hud;
        bool lastA,lastB,lastX,lastY,lastTrigger,lastStick;
        bool displaySubscribed;
        readonly HeadsetPreview preview=new HeadsetPreview();
        public HandPreviewControls handControls;
        readonly List<XRInputSubsystem> inputSubsystems=new List<XRInputSubsystem>();
        bool inputEventsSubscribed;
        string session;
        Vector3 lastRigPosition;Quaternion lastRigRotation;
        void Start(){
            session=Guid.NewGuid().ToString("N");world.displayEnabled=false;
            lastRigPosition=trackingSpace.position;lastRigRotation=trackingSpace.rotation;
            var go=new GameObject("Registration instructions");go.transform.SetParent(eye,false);go.transform.localPosition=new Vector3(0,.36f,1.3f);
            hud=go.AddComponent<TextMesh>();hud.fontSize=64;hud.characterSize=.0045f;hud.anchor=TextAnchor.MiddleCenter;hud.alignment=TextAlignment.Center;hud.color=new Color(1,.75f,.25f);
            var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");hud.font=font;go.GetComponent<Renderer>().sharedMaterial=font.material;
            var reticle=new GameObject("Gaze reticle");reticle.transform.SetParent(eye,false);reticle.transform.localPosition=new Vector3(0,0,1.25f);
            var cross=reticle.AddComponent<TextMesh>();cross.text="+";cross.characterSize=.01f;cross.fontSize=40;cross.anchor=TextAnchor.MiddleCenter;cross.font=font;reticle.GetComponent<Renderer>().sharedMaterial=font.material;
            OVRManager.TrackingLost+=TrackingLost;OVRManager.HMDUnmounted+=Unmounted;TrySubscribeDisplay();Log("session_start",Vector3.zero,0);
            for(int i=0;i<controls.Length;i++)Log("configured_control_unity_"+i,controls[i],0);
            Log("configured_checkpoint_unity",independentCheck,0);Log("configured_probe_offset",probeOffsetInControllerSpace,0);
        }
        void TrySubscribeDisplay(){if(!displaySubscribed&&OVRManager.display!=null){OVRManager.display.RecenteredPose+=Recentered;displaySubscribed=true;}
            if(!inputEventsSubscribed){SubsystemManager.GetSubsystems(inputSubsystems);foreach(var subsystem in inputSubsystems)subsystem.trackingOriginUpdated+=OriginChanged;inputEventsSubscribed=inputSubsystems.Count>0;}}
        void OriginChanged(XRInputSubsystem subsystem)=>Invalidate("XR tracking origin changed");
        void TrackingLost()=>Invalidate("Headset tracking lost");
        void Unmounted()=>Invalidate("Headset removed");
        void Recentered()=>Invalidate("Tracking origin recentered");
        public void Invalidate(string reason){aligned=false;captured.Clear();if(world)preview.Reset(world);if(handControls)handControls.Suspend();status=reason+". Recalibrate.";Log("invalidated: "+reason,Vector3.zero,0);}
        public bool CaptureProbe(Vector3 point){
            if(float.IsNaN(point.x)||float.IsNaN(point.y)||float.IsNaN(point.z)||float.IsInfinity(point.x)||float.IsInfinity(point.y)||float.IsInfinity(point.z)){
                Invalidate("Nonfinite probe position");return false;
            }
            if(aligned){float error=Vector3.Distance(world.transform.TransformPoint(independentCheck),point);Log("independent_checkpoint",point,error);
                if(error>maxCheckM){Invalidate("Independent checkpoint failed: "+(error*1000).ToString("F1")+" mm");return false;}
                status="Checkpoint "+(error*1000).ToString("F1")+" mm. Single check only; NOT layout-grade.";return true;}
            captured.Add(point);Log("control_"+captured.Count,point,0);
            if(captured.Count<3){status="Captured control "+captured.Count+". Move to next control.";return true;}
            if(!ManualRegistration.TrySolve(controls,captured.ToArray(),maxFitM,out var pose,out float rms,out string reason)){Invalidate(reason);return false;}
            world.transform.SetPositionAndRotation(pose.position,pose.rotation);aligned=true;world.displayEnabled=true;
            status="Manual fit RMS "+(rms*1000).ToString("F1")+" mm. Capture independent check next.";
            Log("manual_alignment",pose.position,rms);Log("rotation_euler_degrees",pose.rotation.eulerAngles,0);return true;
        }
        void Update(){
            TrySubscribeDisplay();
            if(!eye||!trackingSpace||!world)return;
            var h=InputDevices.GetDeviceAtXRNode(XRNode.Head);
            bool headValid=h.TryGetFeatureValue(CommonUsages.isTracked,out bool tracked)&&tracked;
            if(headValid&&h.TryGetFeatureValue(CommonUsages.trackingState,out InputTrackingState ht))headValid=(ht&(InputTrackingState.Position|InputTrackingState.Rotation))==(InputTrackingState.Position|InputTrackingState.Rotation);else headValid=false;
            if(!headValid&&(aligned||captured.Count>0||preview.placed))Invalidate("Head pose unavailable");
            if((trackingSpace.position-lastRigPosition).sqrMagnitude>.000001f||Quaternion.Angle(trackingSpace.rotation,lastRigRotation)>.01f){if(aligned||captured.Count>0||preview.placed)Invalidate("Tracking-space transform changed");lastRigPosition=trackingSpace.position;lastRigRotation=trackingSpace.rotation;}
            var r=InputDevices.GetDeviceAtXRNode(XRNode.RightHand);var l=InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            bool controllerAvailable=(OVRInput.GetConnectedControllers()&OVRInput.Controller.RTouch)!=0 && r.TryGetFeatureValue(CommonUsages.isTracked,out bool controllerTracked)&&controllerTracked;
            if(!controllerAvailable&&!aligned&&captured.Count==0){
                if(headValid&&Application.isFocused){
                    hud.text=preview.Tick(world,eye,Time.deltaTime);
                    if(preview.placed&&handControls){handControls.Tick(preview);hud.text+="\n"+handControls.Hint;}
                }
                else {preview.Reset(world);if(handControls)handControls.Suspend();hud.text="SPATIALBUILD - PREVIEW\nWaiting for headset tracking";}
                return;
            }
            if(preview.placed)Invalidate("Controller connected; manual calibration required");
            bool a=Button(r,CommonUsages.primaryButton),b=Button(r,CommonUsages.secondaryButton),x=Button(l,CommonUsages.primaryButton),y=Button(l,CommonUsages.secondaryButton),trigger=Button(r,CommonUsages.triggerButton),stick=Button(l,CommonUsages.primary2DAxisClick);
            if(b&&!lastB)Invalidate("Operator reset");
            if(a&&!lastA){
                bool valid=headValid&&r.TryGetFeatureValue(CommonUsages.isTracked,out bool rt)&&rt&&r.TryGetFeatureValue(CommonUsages.trackingState,out InputTrackingState rs)&&(rs&(InputTrackingState.Position|InputTrackingState.Rotation))==(InputTrackingState.Position|InputTrackingState.Rotation);
                if(valid&&r.TryGetFeatureValue(CommonUsages.devicePosition,out Vector3 p)&&r.TryGetFeatureValue(CommonUsages.deviceRotation,out Quaternion q))CaptureProbe(trackingSpace.TransformPoint(p+q*probeOffsetInControllerSpace));
                else status="Capture refused: valid head and controller poses required.";
            }
            if(x&&!lastX)world.discipline=(Discipline)(((int)world.discipline+1)%6);
            if(y&&!lastY)world.taskOnly=!world.taskOnly;
            if(trigger&&!lastTrigger&&aligned)world.Select(eye.GetComponent<Camera>());
            if(stick&&!lastStick)world.elevationFilter=!world.elevationFilter;
            if(l.TryGetFeatureValue(CommonUsages.primary2DAxis,out Vector2 axis)&&Math.Abs(axis.y)>.6f)world.elevationM=Mathf.Clamp(world.elevationM+Mathf.Sign(axis.y)*.25f*Time.deltaTime,0,10);
            lastA=a;lastB=b;lastX=x;lastY=y;lastTrigger=trigger;lastStick=stick;
            Vector3 next=aligned?independentCheck:controls[Mathf.Min(captured.Count,2)];
            hud.text="SPATIALBUILD - MANUAL DEMO / NOT LAYOUT-GRADE\n"+status+"\n"+
                (aligned?"Independent check":"Control "+(captured.Count+1))+" project XYZ m: "+next.x.ToString("F2")+", "+next.z.ToString("F2")+", "+next.y.ToString("F2")+"\n"+
                "A: capture probe | B: reset | trigger: select\nX: discipline | Y: hanger task | left stick: elevation\nStick click: slice "+(world.elevationFilter?"ON":"OFF")+" | "+world.discipline+" | "+world.elevationM.ToString("F2")+" m\n"+
                (aligned?world.selectedInfo:"Place controller reference at each measured control.\nDefault probe offset = zero; calibrate the physical reference.");
        }
        static bool Button(InputDevice device,InputFeatureUsage<bool> feature)=>device.TryGetFeatureValue(feature,out bool value)&&value;
        void Log(string action,Vector3 value,float error){if(string.IsNullOrEmpty(session))return;try{
            var entry=new RegistrationEvent{utc=DateTime.UtcNow.ToString("O"),session=session,action=action,value=value,errorM=error};
            File.AppendAllText(Path.Combine(Application.persistentDataPath,"registration-"+session+".jsonl"),JsonUtility.ToJson(entry)+"\n");
        }catch(Exception e){Debug.LogWarning("Registration log failed: "+e.Message);}}
        [Serializable] class RegistrationEvent{public string utc,session,action;public Vector3 value;public float errorM;}
        void OnApplicationPause(bool p){if(p)Invalidate("Application paused");}
        void OnApplicationFocus(bool f){if(!f)Invalidate("Application focus lost");}
        void OnDestroy(){OVRManager.TrackingLost-=TrackingLost;OVRManager.HMDUnmounted-=Unmounted;if(displaySubscribed&&OVRManager.display!=null)OVRManager.display.RecenteredPose-=Recentered;foreach(var subsystem in inputSubsystems)subsystem.trackingOriginUpdated-=OriginChanged;}
    }
}
