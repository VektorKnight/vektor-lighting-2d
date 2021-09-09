using System;

namespace VektorLighting2D.Lights {
    [Serializable]
    public enum LightType {
        Point,
        Spot,
        Global,
        Directional
    }
}