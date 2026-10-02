namespace SpatialBuild.Quest {
    // Tracking recovery must include an observed release before another action.
    public sealed class PinchGate {
        bool armed,held;
        public bool Pressed(bool valid,bool pinching) {
            if(!valid){Reset();return false;}
            if(!pinching){armed=true;held=false;return false;}
            if(!armed||held)return false;
            held=true;return true;
        }
        public void Reset(){armed=false;held=false;}
    }
}
