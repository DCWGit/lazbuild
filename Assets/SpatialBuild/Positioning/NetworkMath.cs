using System;
using System.Collections.Generic;

namespace SpatialBuild.Positioning {
    // Double precision, metres, right-handed project coordinates: X east, Y north, Z up.
    [Serializable] public struct Point3 {
        public double x,y,z;
        public Point3(double x,double y,double z){this.x=x;this.y=y;this.z=z;}
        public static Point3 operator +(Point3 a,Point3 b)=>new Point3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Point3 operator -(Point3 a,Point3 b)=>new Point3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Point3 operator *(Point3 a,double s)=>new Point3(a.x*s,a.y*s,a.z*s);
        public double Length=>Math.Sqrt(x*x+y*y+z*z);
    }
    [Serializable] public class Observation {
        public int from,to;
        public double distance,azimuth,elevation,rangeSigma,angleSigma;
    }
    public sealed class NetworkSolution {
        public bool valid;
        public string reason="Not solved";
        public Point3[] positions;
        public double[] residuals,positionSigmaMm;
        public double weightedRms,maxStandardResidual;
        public int iterations,degreesOfFreedom;
    }
    public static class NetworkSolver {
        public static double Wrap(double v)=>Math.Atan2(Math.Sin(v),Math.Cos(v));
        public static double[] Predict(Point3 a,Point3 b) {
            var d=b-a; return new[]{d.Length,Math.Atan2(d.y,d.x),Math.Atan2(d.z,Math.Sqrt(d.x*d.x+d.y*d.y))};
        }
        static bool Finite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x);
        // Gauge: node A fixed at origin; ALL optical-head orientations are independently known.
        // This is not a range-only/self-orienting network solution.
        public static NetworkSolution Solve(int count,IList<Observation> observations) {
            var result=new NetworkSolution(); int n=3*(count-1);
            if(count<3||observations==null||observations.Count*3<=n){result.reason="Insufficient redundant observations";return result;}
            foreach(var o in observations) {
                if(o.from<0||o.to<0||o.from>=count||o.to>=count||o.from==o.to||!Finite(o.distance)||o.distance<=0||
                   !Finite(o.azimuth)||!Finite(o.elevation)||Math.Abs(o.elevation)>=Math.PI/2-.0001||
                   !Finite(o.rangeSigma)||!Finite(o.angleSigma)||o.rangeSigma<=0||o.angleSigma<=0) {
                    result.reason="Invalid observation";return result;
                }
            }
            var x=new double[n];var initialized=new bool[count];initialized[0]=true;
            // Measurement-only initialization. Simulation truth never enters this method.
            for(int pass=0;pass<count;pass++) foreach(var o in observations) {
                if(!initialized[o.from]||initialized[o.to])continue;
                var a=Get(x,o.from);var horizontal=o.distance*Math.Cos(o.elevation);
                Set(x,o.to,a+new Point3(horizontal*Math.Cos(o.azimuth),horizontal*Math.Sin(o.azimuth),o.distance*Math.Sin(o.elevation)));
                initialized[o.to]=true;
            }
            foreach(bool ok in initialized)if(!ok){result.reason="Disconnected network / missing directed initialization path";return result;}
            bool converged=false;
            for(int iteration=0;iteration<30;iteration++) {
                var r=Residual(x,observations);var j=Jacobian(x,observations);Normal(j,r,out var h,out var g);
                if(!Invert(h,out var inv)){result.reason="Rank deficient or ill-conditioned geometry";return result;}
                var step=new double[n];double length=0;
                for(int a=0;a<n;a++){for(int b=0;b<n;b++)step[a]-=inv[a,b]*g[b];length+=step[a]*step[a];}
                result.iterations=iteration+1;
                if(Math.Sqrt(length)<1e-9){converged=true;break;}
                double before=Norm(r),scale=1;bool accepted=false;
                for(int trial=0;trial<12;trial++) {
                    var candidate=(double[])x.Clone();for(int k=0;k<n;k++)candidate[k]+=scale*step[k];
                    if(Norm(Residual(candidate,observations))<before){x=candidate;accepted=true;break;}scale*=.5;
                }
                if(!accepted){if(Math.Sqrt(length)<1e-7){converged=true;break;}result.reason="Solver did not descend";return result;}
            }
            if(!converged){result.reason="Iteration limit";return result;}
            result.residuals=Residual(x,observations);Normal(Jacobian(x,observations),result.residuals,out var normal,out var unused);
            if(!Invert(normal,out var covariance)){result.reason="Covariance unavailable";return result;}
            result.positions=new Point3[count];result.positionSigmaMm=new double[count];
            result.degreesOfFreedom=result.residuals.Length-n;
            result.weightedRms=Math.Sqrt(Norm(result.residuals)/result.degreesOfFreedom);
            // Conditional linearized sqrt(trace(Cov)): 3D RMS, NOT a 95% bound or headset uncertainty.
            for(int i=0;i<count;i++) {
                result.positions[i]=Get(x,i);
                if(i>0){int k=(i-1)*3;result.positionSigmaMm[i]=1000*Math.Sqrt(Math.Max(0,covariance[k,k]+covariance[k+1,k+1]+covariance[k+2,k+2]));}
            }
            foreach(double r in result.residuals)result.maxStandardResidual=Math.Max(result.maxStandardResidual,Math.Abs(r));
            result.valid=true;result.reason="Solved; conditional on fixed A and known station orientations";return result;
        }
        static Point3 Get(double[] x,int i)=>i==0?new Point3():new Point3(x[(i-1)*3],x[(i-1)*3+1],x[(i-1)*3+2]);
        static void Set(double[] x,int i,Point3 p){if(i==0)return;int k=(i-1)*3;x[k]=p.x;x[k+1]=p.y;x[k+2]=p.z;}
        static double[] Residual(double[] x,IList<Observation> os) {
            var r=new double[os.Count*3];int k=0;
            foreach(var o in os){var p=Predict(Get(x,o.from),Get(x,o.to));r[k++]=(p[0]-o.distance)/o.rangeSigma;r[k++]=Wrap(p[1]-o.azimuth)/o.angleSigma;r[k++]=Wrap(p[2]-o.elevation)/o.angleSigma;}return r;
        }
        static double[,] Jacobian(double[] x,IList<Observation> os) {
            int m=os.Count*3;var j=new double[m,x.Length];const double delta=1e-5;
            for(int c=0;c<x.Length;c++){double save=x[c];x[c]=save+delta;var plus=Residual(x,os);x[c]=save-delta;var minus=Residual(x,os);x[c]=save;for(int r=0;r<m;r++)j[r,c]=(plus[r]-minus[r])/(2*delta);}return j;
        }
        static double Norm(double[] r){double s=0;foreach(double v in r)s+=v*v;return s;}
        static void Normal(double[,] j,double[] r,out double[,] h,out double[] g) {
            int n=j.GetLength(1);h=new double[n,n];g=new double[n];
            for(int row=0;row<r.Length;row++)for(int a=0;a<n;a++){g[a]+=j[row,a]*r[row];for(int b=0;b<n;b++)h[a,b]+=j[row,a]*j[row,b];}
        }
        static bool Invert(double[,] input,out double[,] inverse) {
            int n=input.GetLength(0);var a=new double[n,2*n];inverse=new double[n,n];double largest=0;
            for(int i=0;i<n;i++)for(int j=0;j<n;j++){a[i,j]=input[i,j];largest=Math.Max(largest,Math.Abs(a[i,j]));}for(int i=0;i<n;i++)a[i,n+i]=1;
            if(!Finite(largest)||largest==0)return false;
            for(int k=0;k<n;k++) {
                int pivot=k;for(int i=k+1;i<n;i++)if(Math.Abs(a[i,k])>Math.Abs(a[pivot,k]))pivot=i;
                if(!Finite(a[pivot,k])||Math.Abs(a[pivot,k])<largest*1e-12)return false;
                for(int j=0;j<2*n;j++){double t=a[k,j];a[k,j]=a[pivot,j];a[pivot,j]=t;}
                double d=a[k,k];for(int j=0;j<2*n;j++)a[k,j]/=d;
                for(int i=0;i<n;i++)if(i!=k){d=a[i,k];for(int j=0;j<2*n;j++)a[i,j]-=d*a[k,j];}
            }
            for(int i=0;i<n;i++)for(int j=0;j<n;j++){inverse[i,j]=a[i,n+j];if(!Finite(inverse[i,j]))return false;}return true;
        }
    }
}
