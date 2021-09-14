using System.Runtime.InteropServices;
using UnityEngine;

namespace VektorLighting2D.RayMarching.Lights {
    [StructLayout(LayoutKind.Sequential)]
    public struct PointLightData {
        public Vector2 Position;
        public Vector3 Color;
        public float Range;
        public uint Enabled;

        public PointLightData(Vector2 position, Color color, float range, bool enabled) {
            Position = position;
            Color = new Vector3(color.r, color.g, color.b);
            Range = range;
            Enabled = enabled ? 1u : 0;
        }
    }
}