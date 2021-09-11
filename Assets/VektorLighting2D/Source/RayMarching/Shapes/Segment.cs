using System.Runtime.InteropServices;
using UnityEngine;

namespace VektorLighting2D.RayMarching.Shapes {
    [StructLayout(LayoutKind.Sequential)]
    public struct Segment {
        public Vector2 A;
        public Vector2 B;

        public Segment(Vector2 a, Vector2 b) {
            A = a;
            B = b;
        }
    }
}