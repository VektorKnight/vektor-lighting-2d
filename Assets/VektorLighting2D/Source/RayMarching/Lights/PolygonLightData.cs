using System.Runtime.InteropServices;
using UnityEngine;
using VektorLighting2D.RayMarching.Shapes;

namespace VektorLighting2D.RayMarching.Lights {
    [StructLayout(LayoutKind.Sequential)]
    public struct PolygonLightData {
        public Vector3 Color;
        public float Range;
        public uint Offset;
        public uint Length;
        public uint Enabled;

        public PolygonLightData(Color color, float range, uint offset, uint length, bool enabled) {
            Color = new Vector3(color.r, color.g, color.b);
            Range = range;
            Offset = offset;
            Length = length;
            Enabled = enabled ? 1u : 0;
        }

        public PolygonLightData(Color color, float range, PolygonShapeData shapeData, bool enabled) {
            Color = new Vector3(color.r, color.g, color.b);
            Range = range;
            Offset = shapeData.Offset;
            Length = shapeData.Length;
            Enabled = enabled ? 1u : 0;
        }
    }
}