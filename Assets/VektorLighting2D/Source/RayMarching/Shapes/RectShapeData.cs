using System.Runtime.InteropServices;
using UnityEngine;

namespace VektorLighting2D.RayMarching.Shapes {
    [StructLayout(LayoutKind.Sequential)]
    public struct RectShapeData {
        public Vector2 Position;
        public Vector2 Extents;
        public uint Enabled;

        public RectShapeData(Vector2 position, Vector2 extents, bool enabled) {
            Position = position;
            Extents = extents;
            Enabled = enabled ? 1u : 0;
        }
    }
}