using System.Runtime.InteropServices;
using UnityEngine;

namespace VektorLighting2D.RayMarching.Lights {
    /// <summary>
    /// Spot light for ray marching.
    /// The cone/triangle is encoded as a range of dot product values.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SpotLightData {
        public Vector2 Position;
        public Vector3 Color;
        public float Range;
        public Vector2 ConeMin;
        public Vector2 ConeMax;
        public uint Enabled;

        public SpotLightData(Vector2 position, Color color, float range, float angle, Vector2 direction, bool enabled) {
            Position = position;
            Color = new Vector3(color.r, color.g, color.b);
            Range = range;

            angle = Mathf.Clamp(angle, 1f, 179f);

            var halfAngle = angle * 0.5f * Mathf.Deg2Rad;

            var vA = new Vector2(
                direction.x * Mathf.Cos(-halfAngle) - direction.y * Mathf.Sin(-halfAngle),
                direction.x * Mathf.Sin(-halfAngle) + direction.y * Mathf.Cos(-halfAngle)
            ).normalized;

            var vB = new Vector2(
                direction.x * Mathf.Cos(halfAngle) - direction.y * Mathf.Sin(halfAngle),
                direction.x * Mathf.Sin(halfAngle) + direction.y * Mathf.Cos(halfAngle)
            ).normalized;

            ConeMin = vA;
            ConeMax = vB;

            Enabled = enabled ? 1u : 0;
        }
    }
}