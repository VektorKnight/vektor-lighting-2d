using System;
using UnityEngine;
using VektorLighting2D.RayMarching.Shapes;

namespace VektorLighting2D.Components.Shapes {
    public sealed class VektorCircleShape : VektorShape {
        [SerializeField] [Range(float.Epsilon, float.MaxValue)] private float _radius = 1f;

        public CircleShapeData GetShapeData() {
            return new CircleShapeData(transform.position, _radius, enabled);
        }

        private void OnDrawGizmosSelected() {
            var origin = transform.position;

            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(origin, Vector3.one * 0.1f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, _radius);
        }
    }
}