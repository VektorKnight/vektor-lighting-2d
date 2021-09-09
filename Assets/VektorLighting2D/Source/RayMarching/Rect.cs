using System.Runtime.InteropServices;
using UnityEngine;

namespace VektorLighting2D.RayMarching {
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect {
        public Vector2 Position;
        public Vector2 Extents;

        public Rect(Vector2 position, Vector2 extents) {
            Position = position;
            Extents = extents;
        }
    }
}