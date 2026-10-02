using System;
using System.Collections.Generic;

namespace SpatialBuild.Positioning {
    [Serializable] public class NoiseSettings {
        // Independent Gaussian 1-sigma inputs. Shared biases are explicit and excluded from formal covariance.
        public double rangeMm=1,angleDeg=.005,levelDeg=.002,encoderDeg=.001,centeringMm=.2;
        public double persistentRangeBiasMm=0,persistentYawBiasDeg=0;
        public int seed=421;
        public void Validate(){foreach(double v in new[]{rangeMm,angleDeg,levelDeg,encoderDeg,centeringMm})if(double.IsNaN(v)||double.IsInfinity(v)||v<0)throw new ArgumentException("Noise must be finite and nonnegative");
            if(double.IsNaN(persistentRangeBiasMm)||double.IsInfinity(persistentRangeBiasMm)||double.IsNaN(persistentYawBiasDeg)||double.IsInfinity(persistentYawBiasDeg))throw new ArgumentException("Bias must be finite");}
    }
    public static class MeasurementSimulation {
        public static Point3[] DefaultNodes(bool fourth=false)=>fourth?
            new[]{new Point3(),new Point3(10,0,0),new Point3(2,8,.5),new Point3(9,7,1.2)}:
            new[]{new Point3(),new Point3(10,0,0),new Point3(2,8,.5)};
        public static List<Observation> Capture(Point3[] truth,NoiseSettings settings,int sample,bool occluded=false) {
            settings.Validate();var os=new List<Observation>();var random=new Random(unchecked(settings.seed+sample*7919));
            for(int a=0;a<truth.Length;a++)for(int b=0;b<truth.Length;b++)if(a!=b) {
                if(occluded&&(a==2||b==2))continue;
                var p=NetworkSolver.Predict(truth[a],truth[b]);if(p[0]<.1)throw new ArgumentException("Nodes must be at least 0.1 m apart");
                double radians=Math.PI/180;
                double angleSigma=Math.Sqrt(Math.Pow(settings.angleDeg*radians,2)+Math.Pow(settings.encoderDeg*radians,2)+Math.Pow(settings.levelDeg*radians,2)+Math.Pow(settings.centeringMm/1000/p[0],2));
                os.Add(new Observation{from=a,to=b,
                    distance=p[0]+Gaussian(random)*settings.rangeMm/1000+settings.persistentRangeBiasMm/1000,
                    azimuth=NetworkSolver.Wrap(p[1]+Gaussian(random)*angleSigma+settings.persistentYawBiasDeg*radians),
                    elevation=p[2]+Gaussian(random)*angleSigma,
                    rangeSigma=Math.Max(settings.rangeMm/1000,1e-6),angleSigma=Math.Max(angleSigma,1e-9)});
            }
            return os;
        }
        static double Gaussian(Random r)=>Math.Sqrt(-2*Math.Log(Math.Max(1e-15,r.NextDouble())))*Math.Cos(2*Math.PI*r.NextDouble());
        public static double MaxTruthErrorMm(NetworkSolution solution,Point3[] truth) {
            if(!solution.valid)return double.NaN;double max=0;var datum=truth[0];
            for(int i=0;i<truth.Length;i++)max=Math.Max(max,(solution.positions[i]-(truth[i]-datum)).Length*1000);return max;
        }
    }
    public sealed class CalibrationMonitor {
        Point3[] baseline;
        public bool faultLatched{get;private set;}
        public double displacementMm{get;private set;}
        public bool HasBaseline=>baseline!=null;
        public void Reset(){baseline=null;faultLatched=false;displacementMm=0;}
        public void Calibrate(NetworkSolution solution){if(!solution.valid)throw new ArgumentException("Cannot calibrate invalid solution");baseline=(Point3[])solution.positions.Clone();faultLatched=false;displacementMm=0;}
        public void Observe(NetworkSolution solution,double thresholdMm) {
            displacementMm=0;
            if(!solution.valid)return;
            if(solution.maxStandardResidual>5)faultLatched=true;
            if(baseline==null)return;
            if(baseline.Length!=solution.positions.Length){faultLatched=true;return;}
            for(int i=1;i<baseline.Length;i++)displacementMm=Math.Max(displacementMm,(solution.positions[i]-baseline[i]).Length*1000);
            if(displacementMm>thresholdMm||solution.maxStandardResidual>5)faultLatched=true;
        }
    }
}
