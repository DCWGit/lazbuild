// Install Meta XR Core, then add SPATIALBUILD_META_XR to scripting define symbols.
// This thin adapter is not compiled or device-tested in the supplied environment.
#if SPATIALBUILD_META_XR
using UnityEngine;
namespace SpatialBuild {
    public class MetaPinchSelection : MonoBehaviour {
        public OVRHand hand;
        public SpatialBuildViewer viewer;
        bool wasPinching;
        void Update() {
            if(!hand || !viewer || !hand.IsTracked || !hand.IsDataValid || !hand.IsPointerPoseValid ||
                hand.GetFingerConfidence(OVRHand.HandFinger.Index)!=OVRHand.TrackingConfidence.High) {wasPinching=false;return;}
            bool pinching=hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
            if(pinching && !wasPinching) viewer.SelectRay(new Ray(hand.PointerPose.position,hand.PointerPose.forward));
            wasPinching=pinching;
        }
    }
}
#endif
