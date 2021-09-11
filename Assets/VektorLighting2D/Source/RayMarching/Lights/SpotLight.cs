using System.Runtime.InteropServices;
using UnityEngine;

namespace VektorLighting2D.RayMarching.Lights {
    /// <summary>
    /// Spot light for ray marching.
    /// The cone/triangle is encoded as a range of dot product values.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SpotLight {
        public Vector2 Position;
        public Vector3 Color;
        public float Radius;
        public Vector4 Cone;

        public SpotLight(Vector2 position, Vector3 color, float radius, Vector4 cone) {
            Position = position;
            Color = color;
            Radius = radius;
            Cone = cone;
        }
        
        public SpotLight(Vector2 position, Vector3 color, float radius, float angle, Vector2 direction) {
            Position = position;
            Color = color;
            Radius = radius;

            var halfAngle = angle * 0.5f * Mathf.Deg2Rad;
            //rotation *= Mathf.Deg2Rad;
            
            var vA = new Vector2(
                direction.x * Mathf.Cos(-halfAngle) - direction.y * Mathf.Sin(-halfAngle),
                direction.x * Mathf.Sin(-halfAngle) + direction.y * Mathf.Cos(-halfAngle)
            ).normalized;

            var vB = new Vector2(
                direction.x * Mathf.Cos(halfAngle) - direction.y * Mathf.Sin(halfAngle),
                direction.x * Mathf.Sin(halfAngle) + direction.y * Mathf.Cos(halfAngle)
            ).normalized;

            Cone = new Vector4(vA.x, vA.y, vB.x, vB.y);
        }
    }
}