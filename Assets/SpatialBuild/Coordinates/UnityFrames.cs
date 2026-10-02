using UnityEngine;
namespace SpatialBuild.Coordinates {
 public static class UnityFrames {
  // Canonical frames are right-handed XYZ, Z up. Unity is reflected (X,Z,Y).
  public static Vector3 ToUnity(DVec p){if(!p.Finite||p.Norm>10000)throw new System.ArgumentException("Rebase before float conversion");return new Vector3((float)p.x,(float)p.z,(float)p.y);}
  public static DVec FromUnity(Vector3 p)=>new DVec(p.x,p.z,p.y);
  public static Quaternion ToUnity(DQuat q)=>new Quaternion((float)-q.x,(float)-q.z,(float)-q.y,(float)q.w);
  public static DQuat FromUnity(Quaternion q)=>new DQuat(-q.x,-q.z,-q.y,q.w);
  public static RigidD FromUnity(Pose p)=>new RigidD(FromUnity(p.position),FromUnity(p.rotation));
  public static void Apply(Transform root,RigidD pose){root.SetPositionAndRotation(ToUnity(pose.position),ToUnity(pose.rotation));root.localScale=Vector3.one;}
 }
}
