using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpatialBuild.Positioning {
    public enum OpticalStage { Rotate, Search, Detect, Center, Lock, Range }
    public sealed class SimulatedPositioningProvider : PositioningProvider {
        public NoiseSettings noise=new NoiseSettings();
        public bool fourthNode,occludeC;
        public float movementThresholdMm=10,staleSeconds=4;
        public bool scanRunning=true;
        public Point3[] Truth {get;private set;}
        public List<Observation> Measurements {get;private set;}=new List<Observation>();
        public OpticalStage Stage {get;private set;}
        public CalibrationMonitor Monitor {get;}=new CalibrationMonitor();
        public int Sample {get;private set;}
        public double TruthErrorMm {get;private set;}=double.NaN;
        public NetworkSolution Solution {get;private set;}
        public NoiseSettings CapturedNoise {get;private set;}
        PositioningSnapshot snapshot=new PositioningSnapshot{status=PositionStatus.Unknown,synthetic=true,message="No measurement"};
        double stageAt;
        string session;
        public override PositioningSnapshot Current {
            get {
                if(Time.realtimeSinceStartupAsDouble-snapshot.capturedAt>staleSeconds)
                    return new PositioningSnapshot{status=PositionStatus.Unknown,synthetic=true,message="Measurements stale",session=session};
                return snapshot;
            }
        }
        void Awake(){ResetExperiment();}
        public void ResetExperiment(){Truth=MeasurementSimulation.DefaultNodes(fourthNode);session=Guid.NewGuid().ToString("N");Sample=0;Monitor.Reset();Stage=OpticalStage.Rotate;stageAt=Time.realtimeSinceStartupAsDouble;Capture();}
        public void MoveC(){Truth[2]=Truth[2]+new Point3(.05,0,0);Capture();}
        public void RestoreC(){Truth[2]=MeasurementSimulation.DefaultNodes(fourthNode)[2];Capture();}
        public void Calibrate(){if(Solution==null||!Solution.valid||Current.status==PositionStatus.Unknown)return;Monitor.Calibrate(Solution);Publish();}
        public void Capture(){
            try {CapturedNoise=JsonUtility.FromJson<NoiseSettings>(JsonUtility.ToJson(noise));Measurements=MeasurementSimulation.Capture(Truth,CapturedNoise,Sample++,occludeC);Solution=NetworkSolver.Solve(Truth.Length,Measurements);TruthErrorMm=MeasurementSimulation.MaxTruthErrorMm(Solution,Truth);Monitor.Observe(Solution,movementThresholdMm);Publish();}
            catch(Exception e){Measurements=new List<Observation>();Solution=new NetworkSolution{reason=e.Message};TruthErrorMm=double.NaN;Publish();}
        }
        void Publish(){bool valid=Solution!=null&&Solution.valid;
            snapshot=new PositioningSnapshot{synthetic=true,session=session,capturedAt=Time.realtimeSinceStartupAsDouble,network=Solution,
                status=!valid?PositionStatus.Unknown:Monitor.faultLatched?PositionStatus.Fault:PositionStatus.Simulated,
                message=!valid?Solution.reason:Monitor.faultLatched?"Network changed / fault latched. Check controls before recalibration.":"SIMULATION ONLY - physical registration unavailable"};}
        void Update(){if(!scanRunning)return;double now=Time.realtimeSinceStartupAsDouble;if(now-stageAt<.35)return;stageAt=now;Stage=(OpticalStage)(((int)Stage+1)%6);if(Stage==OpticalStage.Range)Capture();}
    }
}
