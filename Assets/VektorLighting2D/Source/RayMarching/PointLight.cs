using UnityEngine;

namespace VektorLighting2D.RayMarching {
    public struct PointLight {
        public Vector2 Position;
        public Vector3 Color;
        public float Radius;

        public PointLight(Vector2 position, Vector3 color, float radius) {
            Position = position;
            Color = color;
            Radius = radius;
        }
    }
}