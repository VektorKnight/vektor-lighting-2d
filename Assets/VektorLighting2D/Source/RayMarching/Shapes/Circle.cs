using System.Runtime.InteropServices;
using UnityEngine;

namespace VektorLighting2D.RayMarching.Shapes {
    [StructLayout(LayoutKind.Sequential)]
    public struct Circle {
        public Vector2 Position;
        public float Radius;

        public Circle(Vector2 position, float radius) {
            Position = position;
            Radius = radius;
        }
    }
}