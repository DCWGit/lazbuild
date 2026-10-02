using System;
using System.Collections.Generic;
using System.Text;

namespace SpatialBuild.Positioning {
    public static class NetworkVerification {
        static void Check(bool ok,string message){if(!ok)throw new Exception("FAILED: "+message);}
        public static string Run(){var report=new StringBuilder();int tests=0;
            Action<string,Action> test=(name,body)=>{body();tests++;report.AppendLine("PASS "+name);};
            var zero=new NoiseSettings{rangeMm=0,angleDeg=0,encoderDeg=0,levelDeg=0,centeringMm=0};
            test("Noiseless 3D triangle and four-node network",()=>{foreach(bool fourth in new[]{false,true}){var t=MeasurementSimulation.DefaultNodes(fourth);var s=NetworkSolver.Solve(t.Length,MeasurementSimulation.Capture(t,zero,0));Check(s.valid,s.reason);Check(MeasurementSimulation.MaxTruthErrorMm(s,t)<.0001,"exact recovery");}});
            test("Nonzero station heights and azimuth wrap",()=>{var t=new[]{new Point3(),new Point3(-8,.00001,2),new Point3(-3,-6,-1)};var s=NetworkSolver.Solve(3,MeasurementSimulation.Capture(t,zero,0));Check(s.valid,s.reason);Check(MeasurementSimulation.MaxTruthErrorMm(s,t)<.0001,"wrapped recovery");});
            test("Deterministic noise and sample variation",()=>{var t=MeasurementSimulation.DefaultNodes();var n=new NoiseSettings();var a=MeasurementSimulation.Capture(t,n,1);var b=MeasurementSimulation.Capture(t,n,1);var c=MeasurementSimulation.Capture(t,n,2);Check(a[0].distance==b[0].distance,"seed repeat");Check(a[0].distance!=c[0].distance,"sample progression");});
            test("Disconnected graph rejected",()=>{var t=MeasurementSimulation.DefaultNodes();var s=NetworkSolver.Solve(3,MeasurementSimulation.Capture(t,zero,0,true));Check(!s.valid,"occlusion must fail");});
            test("Nonfinite observations rejected",()=>{var t=MeasurementSimulation.DefaultNodes();var os=MeasurementSimulation.Capture(t,zero,0);os[0].azimuth=double.NaN;Check(!NetworkSolver.Solve(3,os).valid,"NaN rejection");});
            test("Negative noise rejected",()=>{bool rejected=false;try{MeasurementSimulation.Capture(MeasurementSimulation.DefaultNodes(),new NoiseSettings{rangeMm=-1},0);}catch(ArgumentException){rejected=true;}Check(rejected,"negative sigma");});
            test("Movement alarm latches after C restored",()=>{var t=MeasurementSimulation.DefaultNodes();var monitor=new CalibrationMonitor();monitor.Calibrate(NetworkSolver.Solve(3,MeasurementSimulation.Capture(t,zero,0)));t[2]+=new Point3(.05,0,0);var moved=NetworkSolver.Solve(3,MeasurementSimulation.Capture(t,zero,1));monitor.Observe(moved,10);Check(monitor.faultLatched,"move detected");var restored=NetworkSolver.Solve(3,MeasurementSimulation.Capture(MeasurementSimulation.DefaultNodes(),zero,2));monitor.Observe(restored,10);Check(monitor.faultLatched,"restore does not silently clear");monitor.Calibrate(restored);Check(!monitor.faultLatched,"explicit reset");});
            test("Inconsistent range produces large residual",()=>{var t=MeasurementSimulation.DefaultNodes();var n=new NoiseSettings();var os=MeasurementSimulation.Capture(t,n,0);os[0].distance+=.1;var s=NetworkSolver.Solve(3,os);Check(!s.valid||s.maxStandardResidual>5,"bad observation not clean");});
            test("Shared orientation bias can hide behind small residuals",()=>{var n=new NoiseSettings{rangeMm=.001,angleDeg=.000001,encoderDeg=0,levelDeg=0,centeringMm=0,persistentYawBiasDeg=.1};var t=MeasurementSimulation.DefaultNodes();var s=NetworkSolver.Solve(3,MeasurementSimulation.Capture(t,n,0));Check(s.valid,s.reason);Check(MeasurementSimulation.MaxTruthErrorMm(s,t)>10,"bias creates actual error");Check(s.weightedRms<3,"internal consistency is not absolute accuracy");});
            test("Monte Carlo independent-noise conditional RMS",()=>{double errors=0,formal=0;int count=0;var t=MeasurementSimulation.DefaultNodes(true);var n=new NoiseSettings();for(int k=0;k<200;k++){var s=NetworkSolver.Solve(4,MeasurementSimulation.Capture(t,n,k));Check(s.valid,s.reason);for(int i=1;i<4;i++){errors+=Math.Pow((s.positions[i]-t[i]).Length*1000,2);formal+=s.positionSigmaMm[i]*s.positionSigmaMm[i];count++;}}double ratio=Math.Sqrt(errors/formal);Check(ratio>.7&&ratio<1.3,"empirical/formal RMS ratio "+ratio);report.AppendLine("  200 synthetic networks; empirical RMS "+Math.Sqrt(errors/count).ToString("F3")+" mm; formal RMS "+Math.Sqrt(formal/count).ToString("F3")+" mm; ratio "+ratio.ToString("F3"));});
            report.AppendLine(tests+" verification groups passed. Synthetic data only.");return report.ToString();
        }
    }
}
