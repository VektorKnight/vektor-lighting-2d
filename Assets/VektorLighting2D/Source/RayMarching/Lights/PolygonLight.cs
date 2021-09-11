using System.Runtime.InteropServices;
using UnityEngine;
using VektorLighting2D.RayMarching.Shapes;

namespace VektorLighting2D.RayMarching.Lights {
    [StructLayout(LayoutKind.Sequential)]
    public struct PolygonLight {
        public Vector3 Color;
        public float Radius;
        public Polygon Polygon;

        public PolygonLight(Vector3 color, float radius, Polygon polygon) {
            Color = color;
            Radius = radius;
            Polygon = polygon;
        }
    }
}