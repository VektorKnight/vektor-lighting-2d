using System.Collections.Generic;
using UnityEngine;
using VektorLighting2D.RayMarching.Shapes;

namespace VektorLighting2D.Components.Shapes {
    /// <summary>
    /// Defines and visualizes a polygon for shapes and lights.
    /// </summary>
    public sealed class VektorPolygonShape : VektorShape {
        [Header("Polygon")]
        [SerializeField] private Vector2[] _vertices;

        private List<Vector2> _triangulated;

        public PolygonShapeData GetPolygonData(in List<Segment> segmentBuffer) {
            _triangulated.Clear();
            Triangulate.Process(_vertices, _triangulated);
            
            var offset = segmentBuffer.Count;
            var written = CopySegments(segmentBuffer);
            return new PolygonShapeData((uint)offset, (uint)written, enabled);
        }

        private int CopySegments(in List<Segment> _destination) {
            var written = 0;
            for (var i = 0; i < _triangulated.Count - 1; i += 3) {
                var origin = (Vector2)transform.position;
                _destination.Add(new Segment(origin + _triangulated[i], origin + _triangulated[i + 1]));
                _destination.Add(new Segment(origin + _triangulated[i + 1], origin + _triangulated[i + 2]));
                _destination.Add(new Segment(origin + _triangulated[i + 2], origin + _triangulated[i]));
                written += 3;
            }

            return written;
        }

        protected override void Awake() {
            _triangulated = new List<Vector2>();
            Triangulate.Process(_vertices, _triangulated);
            
            base.Awake();
        }

        private void OnDrawGizmosSelected() {
            if (_triangulated != null && _triangulated.Count > 1) {
                Gizmos.color = Color.white;
                foreach (var vertex in _triangulated) {
                    Gizmos.DrawWireCube(transform.position + (Vector3)vertex, Vector3.one * 0.1f);
                }
            
                Gizmos.color = Color.yellow;
                for (var i = 0; i < _triangulated.Count - 1; i += 3) {
                    Gizmos.DrawLine(transform.position + (Vector3)_triangulated[i], transform.position + (Vector3)_triangulated[i + 1]);
                    Gizmos.DrawLine(transform.position + (Vector3)_triangulated[i + 1], transform.position + (Vector3)_triangulated[i + 2]);
                    Gizmos.DrawLine(transform.position + (Vector3)_triangulated[i + 2], transform.position + (Vector3)_triangulated[i]);
                }
                //Gizmos.DrawLine(transform.position + (Vector3)_triangulated[0], transform.position + (Vector3)_triangulated[_triangulated.Count - 1]);
            }
            else {
                Gizmos.color = Color.white;
                foreach (var vertex in _vertices) {
                    Gizmos.DrawWireCube(transform.position + (Vector3)vertex, Vector3.one * 0.1f);
                }
            
                Gizmos.color = Color.yellow;
                for (var i = 0; i < _vertices.Length - 1; i++) {
                    Gizmos.DrawLine(transform.position + (Vector3)_vertices[i], transform.position + (Vector3)_vertices[i + 1]);
                }
                Gizmos.DrawLine(transform.position + (Vector3)_vertices[0], transform.position + (Vector3)_vertices[_vertices.Length - 1]);
            }
        }
    }
}