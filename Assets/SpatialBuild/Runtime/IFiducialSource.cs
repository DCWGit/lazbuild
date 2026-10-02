using System;
using UnityEngine;
namespace SpatialBuild {
    [Serializable] public class FiducialObservation {
        public string nodeId,markerFamily,trackingSession,calibrationVersion;
        public int markerId,imageWidth,imageHeight;
        public double captureMonotonicSeconds;
        public double[] cornerPixels, cameraIntrinsics, distortion;
        public double[] cameraToTrackingAtCapture, tagToCamera;
        public double markerSideMetres,reprojectionErrorPixels;
        public bool hasUncertaintyBound;
        public double uncertaintyBoundMillimetres;
    }
    public interface IFiducialSource {
        // A future PCA + native detector adapter raises timestamped observations.
        // This interface does not imply that a detector/sensor is implemented.
        event Action<FiducialObservation> Observed;
        bool IsAvailable {get;}
        string UnavailableReason {get;}
    }
}
