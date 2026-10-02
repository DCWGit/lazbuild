using UnityEngine;
using SpatialBuild.Construction;

namespace SpatialBuild.Quest {
    // A reduced-scale inspection view, with no survey or registration claim.
    public sealed class HeadsetPreview {
        public bool placed { get; private set; }
        float steadyTime;
        Vector3 lastPosition;
        Quaternion lastRotation;
        bool initialized;
        public bool fullScale { get; private set; }
        public void Place(ConstructionWorld world,Transform eye,Transform trackingSpace,bool full) {
            Vector3 forward=Vector3.ProjectOnPlane(eye.forward,Vector3.up).normalized;
            if(forward.sqrMagnitude<.5f)return;
            fullScale=full;
            Quaternion rotation=Quaternion.LookRotation(forward,Vector3.up);
            Vector3 centre=full?new Vector3(eye.position.x,trackingSpace.position.y,eye.position.z)+forward*5f:
                eye.position+forward*1.3f-Vector3.up*.55f;
            float scale=full?1f:.1f;
            world.transform.SetPositionAndRotation(centre-rotation*new Vector3(5*scale,0,4*scale),rotation);
            world.transform.localScale=Vector3.one*scale;world.displayEnabled=true;placed=true;
        }
        public void Rotate(ConstructionWorld world,float degrees=45) {
            Vector3 centre=world.transform.TransformPoint(new Vector3(5,0,4));
            world.transform.RotateAround(centre,Vector3.up,degrees);
        }
        public void Reset(ConstructionWorld world) {
            placed=false;steadyTime=0;initialized=false;fullScale=false;
            world.displayEnabled=false;world.transform.localScale=Vector3.one;
        }
        public string Tick(ConstructionWorld world, Transform eye, float dt) {
            if(!placed) {
                if(!initialized || Vector3.Distance(eye.position,lastPosition)>.04f || Quaternion.Angle(eye.rotation,lastRotation)>6f) {
                    lastPosition=eye.position;lastRotation=eye.rotation;steadyTime=0;initialized=true;
                } else steadyTime+=dt;
                if(steadyTime<3f)return "UNREGISTERED PREVIEW\nHold your head still: "+Mathf.CeilToInt(3f-steadyTime)+"\nNo controllers required";
                Vector3 forward=Vector3.ProjectOnPlane(eye.forward,Vector3.up);
                if(forward.sqrMagnitude<.01f)return "UNREGISTERED PREVIEW\nLook toward the room to place preview";
                Quaternion rotation=Quaternion.LookRotation(forward.normalized,Vector3.up);
                // Centre the 10 x 8 m model as a 1 x 0.8 m miniature ahead of the user.
                Vector3 centre=eye.position+forward.normalized*1.3f-Vector3.up*.55f;
                world.transform.SetPositionAndRotation(centre-rotation*new Vector3(.5f,0,.4f),rotation);
                world.transform.localScale=Vector3.one*.1f;
                world.discipline=Discipline.All;world.elevationFilter=false;world.taskOnly=false;
                world.displayEnabled=true;placed=true;
                Debug.Log("SpatialBuild: controller-free unregistered preview placed at 1:10 scale.");
            }
            if(world.jobActive&&world.job!=null)return "DEMO - POSITION NOT VERIFIED\n"+(world.job.Current==null?"All steps marked done":world.job.Current.targetId+" | "+world.job.Current.title)+"\n"+world.job.done.Count+" / "+world.job.definition.steps.Length+" marked done";
            return "SPATIALBUILD | NOT REGISTERED\nOpen MENU > JOB > START for the work guide";
        }
    }
}
