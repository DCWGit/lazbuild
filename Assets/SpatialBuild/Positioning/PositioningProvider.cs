using UnityEngine;

namespace SpatialBuild.Positioning {
    public enum PositionStatus { Unknown, Simulated, Fault }
    public sealed class PositioningSnapshot {
        public PositionStatus status;
        public bool synthetic;
        public string message,session;
        public double capturedAt;
        public NetworkSolution network;
        // Absolute project-to-headset registration is intentionally absent until independently calibrated.
    }
    public abstract class PositioningProvider : MonoBehaviour {
        public abstract PositioningSnapshot Current {get;}
    }
    public sealed class RealPositioningProvider : PositioningProvider {
        // Hardware adapter boundary; fail closed until transport, calibration and uncertainty are implemented.
        readonly PositioningSnapshot unavailable=new PositioningSnapshot{status=PositionStatus.Unknown,synthetic=false,message="Hardware provider not connected"};
        public override PositioningSnapshot Current=>unavailable;
    }
}
