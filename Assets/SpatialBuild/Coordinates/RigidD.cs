using System;
namespace SpatialBuild.Coordinates {
 [Serializable] public struct DVec {
  public double x,y,z; public DVec(double x,double y,double z){this.x=x;this.y=y;this.z=z;}
  public static DVec operator +(DVec a,DVec b)=>new DVec(a.x+b.x,a.y+b.y,a.z+b.z);
  public static DVec operator -(DVec a,DVec b)=>new DVec(a.x-b.x,a.y-b.y,a.z-b.z);
  public static DVec operator *(DVec a,double s)=>new DVec(a.x*s,a.y*s,a.z*s);
  public double Norm=>Math.Sqrt(x*x+y*y+z*z);
  public bool Finite=>Number(x)&&Number(y)&&Number(z);
  public static bool Number(double v)=>!double.IsNaN(v)&&!double.IsInfinity(v);
  public static double Dot(DVec a,DVec b)=>a.x*b.x+a.y*b.y+a.z*b.z;
  public static DVec Cross(DVec a,DVec b)=>new DVec(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
 }
 [Serializable] public struct DQuat {
  public double x,y,z,w;
  public DQuat(double x,double y,double z,double w){double n=Math.Sqrt(x*x+y*y+z*z+w*w);if(!DVec.Number(n)||n<1e-12)throw new ArgumentException("Invalid quaternion");this.x=x/n;this.y=y/n;this.z=z/n;this.w=w/n;}
  public static DQuat Identity=>new DQuat(0,0,0,1);
  public DQuat Inverse=>new DQuat(-x,-y,-z,w);
  public static DQuat operator *(DQuat a,DQuat b)=>new DQuat(a.w*b.x+a.x*b.w+a.y*b.z-a.z*b.y,a.w*b.y-a.x*b.z+a.y*b.w+a.z*b.x,a.w*b.z+a.x*b.y-a.y*b.x+a.z*b.w,a.w*b.w-a.x*b.x-a.y*b.y-a.z*b.z);
  public DVec Rotate(DVec p){var q=new DVec(x,y,z);var t=DVec.Cross(q,p)*2;return p+t*w+DVec.Cross(q,t);}
  public static double Angle(DQuat a,DQuat b){double dot=Math.Abs(a.x*b.x+a.y*b.y+a.z*b.z+a.w*b.w);return 2*Math.Acos(Math.Min(1,dot));}
  public static DQuat Blend(DQuat a,DQuat b,double f){if(a.x*b.x+a.y*b.y+a.z*b.z+a.w*b.w<0)b=new DQuat(-b.x,-b.y,-b.z,-b.w);return new DQuat(a.x*(1-f)+b.x*f,a.y*(1-f)+b.y*f,a.z*(1-f)+b.z*f,a.w*(1-f)+b.w*f);}
 }
 // ToFrom naming: aFromB maps B coordinates into A. Composition applies right operand first.
 [Serializable] public struct RigidD {
  public DVec position;public DQuat rotation;
  public RigidD(DVec p,DQuat q){if(!p.Finite)throw new ArgumentException("Invalid translation");position=p;rotation=new DQuat(q.x,q.y,q.z,q.w);}
  public static RigidD Identity=>new RigidD(new DVec(),DQuat.Identity);
  public DVec Apply(DVec p)=>rotation.Rotate(p)+position;
  public RigidD Inverse{get{var q=rotation.Inverse;return new RigidD(q.Rotate(position*-1),q);}}
  public static RigidD operator *(RigidD a,RigidD b)=>new RigidD(a.Apply(b.position),a.rotation*b.rotation);
  public static RigidD Blend(RigidD a,RigidD b,double f)=>new RigidD(a.position*(1-f)+b.position*f,DQuat.Blend(a.rotation,b.rotation,f));
 }
 public sealed class CoordinateChain {
  public RigidD ProjectFromBim=RigidD.Identity,QuestFromProject=RigidD.Identity,QuestFromHead=RigidD.Identity;
  public DVec ProjectOrigin;
  public DVec BimToQuest(DVec p)=>QuestFromProject.Apply(ProjectFromBim.Apply(p));
  public DVec QuestToProject(DVec p)=>QuestFromProject.Inverse.Apply(p);
  public RigidD QuestFromProjectLocal=>QuestFromProject*new RigidD(ProjectOrigin,DQuat.Identity);
  public RigidD ProjectFromHead=>QuestFromProject.Inverse*QuestFromHead;
  public DVec Local(DVec project)=>project-ProjectOrigin;
 }
}
