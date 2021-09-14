using System;
using UnityEngine;
using VektorLighting2D.RayMarching.Shapes;

namespace VektorLighting2D.Components.Shapes {
    public sealed class VektorRectShape : VektorShape {
        [SerializeField] private Vector2 _size = new Vector2(1f, 1f);

        public RectShapeData GetShapeData() {
            var extents = _size * 0.5f;
            return new RectShapeData(transform.position, extents, enabled);
        }

        private void OnDrawGizmosSelected() {
            var origin = transform.position;
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(origin, Vector3.one * 0.1f);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(origin, _size);
        }
    }
}