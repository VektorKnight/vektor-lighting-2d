using System.Runtime.InteropServices;
using UnityEngine;

namespace VektorLighting2D.RayMarching.Shapes {
    [StructLayout(LayoutKind.Sequential)]
    public struct PolygonShapeData {
        public uint Offset;
        public uint Length;
        public uint Enabled;

        public PolygonShapeData(uint offset, uint length, bool enabled) {
            Offset = offset;
            Length = length;
            Enabled = enabled ? 1u : 0;
        }
    }
}