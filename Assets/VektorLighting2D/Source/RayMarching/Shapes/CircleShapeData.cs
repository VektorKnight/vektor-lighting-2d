using System.Runtime.InteropServices;
using UnityEngine;

namespace VektorLighting2D.RayMarching.Shapes {
    [StructLayout(LayoutKind.Sequential)]
    public struct CircleShapeData {
        public Vector2 Position;
        public float Radius;
        public uint Enabled;

        public CircleShapeData(Vector2 position, float radius, bool enabled) {
            Position = position;
            Radius = radius;
            Enabled = enabled ? 1u : 0;
        }
    }
}