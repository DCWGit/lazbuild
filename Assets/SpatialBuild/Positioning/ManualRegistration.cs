using System;
using UnityEngine;

namespace SpatialBuild.Positioning {
    public static class ManualRegistration {
        public static bool TrySolve(Vector3[] project,Vector3[] tracking,float maximumResidualM,out Pose pose,out float rms,out string reason){
            pose=new Pose(Vector3.zero,Quaternion.identity);rms=float.PositiveInfinity;reason="Need three matching points";
            if(project==null||tracking==null||project.Length!=3||tracking.Length!=3)return false;
            if(!Finite(maximumResidualM)||maximumResidualM<=0){reason="Invalid residual threshold";return false;}
            for(int i=0;i<3;i++)if(!Finite(project[i])||!Finite(tracking[i])){reason="Nonfinite control";return false;}
            if(!Basis(project,out var source)||!Basis(tracking,out var target)){reason="Controls too close or nearly collinear";return false;}
            var rotation=target*Quaternion.Inverse(source);
            Vector3 pc=(project[0]+project[1]+project[2])/3,tc=(tracking[0]+tracking[1]+tracking[2])/3;
            var position=tc-rotation*pc;double squared=0;
            for(int i=0;i<3;i++)squared+=(rotation*project[i]+position-tracking[i]).sqrMagnitude;
            rms=(float)Math.Sqrt(squared/3);if(rms>maximumResidualM){reason="Control fit exceeds threshold";return false;}
            pose=new Pose(position,rotation);reason="Manual alignment only; independent checkpoint required";return true;
        }
        static bool Basis(Vector3[] p,out Quaternion q){q=Quaternion.identity;var x=p[1]-p[0];var other=p[2]-p[0];if(x.magnitude<.25f||other.magnitude<.25f)return false;
            var up=Vector3.Cross(other,x);if(up.magnitude/(x.magnitude*other.magnitude)<.25f)return false;
            x.Normalize();up.Normalize();var forward=Vector3.Cross(x,up);q=Quaternion.LookRotation(forward,up);return true;}
        static bool Finite(float x)=>!float.IsNaN(x)&&!float.IsInfinity(x);
        static bool Finite(Vector3 v)=>Finite(v.x)&&Finite(v.y)&&Finite(v.z);
    }
}
