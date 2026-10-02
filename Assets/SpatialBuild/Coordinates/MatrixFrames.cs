using System;
namespace SpatialBuild.Coordinates {
 public static class MatrixFrames {
  // Row-major homogeneous projectFromBim. Scale, shear and reflection are
  // rejected: those need an explicitly calibrated affine conversion.
  public static RigidD FromRowMajor(double[] m){
   if(m==null||m.Length!=16)throw new ArgumentException("4x4 transform required");
   foreach(var v in m)if(!DVec.Number(v))throw new ArgumentException("Nonfinite BIM transform");
   if(Math.Abs(m[12])>1e-9||Math.Abs(m[13])>1e-9||Math.Abs(m[14])>1e-9||Math.Abs(m[15]-1)>1e-9)throw new ArgumentException("Invalid homogeneous row");
   var x=new DVec(m[0],m[4],m[8]);var y=new DVec(m[1],m[5],m[9]);var z=new DVec(m[2],m[6],m[10]);
   if(Math.Abs(x.Norm-1)>1e-6||Math.Abs(y.Norm-1)>1e-6||Math.Abs(z.Norm-1)>1e-6||Math.Abs(DVec.Dot(x,y))>1e-6||Math.Abs(DVec.Dot(x,z))>1e-6||Math.Abs(DVec.Dot(y,z))>1e-6||DVec.Dot(DVec.Cross(x,y),z)<.999999)throw new ArgumentException("BIM transform must be rigid right-handed");
   double qx,qy,qz,qw,trace=m[0]+m[5]+m[10];
   if(trace>0){double s=Math.Sqrt(trace+1)*2;qw=s*.25;qx=(m[9]-m[6])/s;qy=(m[2]-m[8])/s;qz=(m[4]-m[1])/s;}
   else if(m[0]>m[5]&&m[0]>m[10]){double s=Math.Sqrt(1+m[0]-m[5]-m[10])*2;qw=(m[9]-m[6])/s;qx=s*.25;qy=(m[1]+m[4])/s;qz=(m[2]+m[8])/s;}
   else if(m[5]>m[10]){double s=Math.Sqrt(1+m[5]-m[0]-m[10])*2;qw=(m[2]-m[8])/s;qx=(m[1]+m[4])/s;qy=s*.25;qz=(m[6]+m[9])/s;}
   else {double s=Math.Sqrt(1+m[10]-m[0]-m[5])*2;qw=(m[4]-m[1])/s;qx=(m[2]+m[8])/s;qy=(m[6]+m[9])/s;qz=s*.25;}
   return new RigidD(new DVec(m[3],m[7],m[11]),new DQuat(qx,qy,qz,qw));
  }
 }
}
