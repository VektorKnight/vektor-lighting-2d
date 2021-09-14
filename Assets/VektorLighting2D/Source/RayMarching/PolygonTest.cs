using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.PlayerLoop;
using VektorLighting2D.RayMarching.Shapes;

namespace VektorLighting2D.RayMarching {
    public class PolygonTest : MonoBehaviour {
        public ComputeShader TestShader;
        
        private static readonly PolygonShapeData PolygonShapeData = new PolygonShapeData(0, 3, true);
        private static readonly Segment[] _segments = new[] {
            new Segment(
                new Vector2(0.25f, 0.25f) * 10, 
                new Vector2(0.5f, 0.5f) * 10
            ),
            new Segment(
                new Vector2(0.5f, 0.5f) * 10,
                new Vector2(0.75f, 0.25f) * 10
            ),
            new Segment(
                new Vector2(0.75f, 0.25f) * 10,
                new Vector2(0.25f, 0.25f) * 10
            ),
        };

        private Vector2 _mousePos;
        private Vector2 _nearest;

        private Vector4[] _output;
        
        public static float PositionAlongSegment(Segment s, Vector2 p) {
            return Vector2.Dot(p - s.A, s.B - s.A) / (s.B - s.A).sqrMagnitude;
        }
        
        public static Vector2 PointAlongSegment(Segment s, float sigma) {
            sigma = Mathf.Clamp01(sigma);
            return s.A + sigma * (s.B - s.A);
        }

        public static Vector2 PointAlongPolygon(PolygonShapeData poly, Vector2 p) {
            Vector2 nearest = Vector2.zero;
            float shortest = float.MaxValue;
            for (var i = poly.Offset; i < poly.Offset + poly.Length; i++) {
                var segment = _segments[i];
                var sigma = PositionAlongSegment(segment, p);
                var point = PointAlongSegment(segment, sigma);
                var dist = (p - point).magnitude;

                if (dist < shortest) {
                    nearest = point;
                    shortest = dist;
                }
            }

            return nearest;
        }
        
        float PolygonSDF(PolygonShapeData poly, Segment[] segments, Vector2 p) {
            var v0 = segments[poly.Offset].A;
            var d = Vector2.Dot(p- v0, p - v0);
            var s = 1.0f;
            for (var i = poly.Offset; i < poly.Offset + poly.Length; i++) {
                var seg = segments[i];
                // distance
                var e = seg.A - seg.B;
                var w = p - seg.A;
                var b = w - e * Mathf.Clamp( Vector2.Dot(w,e) / Vector2.Dot(e,e), 0.0f, 1.0f );
                d = Mathf.Min( d, Vector2.Dot(b,b) );

                // winding number from http://geomalgorithms.com/a03-_inclusion.html
                var c1 = p.y >= seg.A.y;
                var c2 = p.y < seg.B.y;
                var c3 = e.x * w.y > e.y * w.x;
                if( c1 && c2 && c3 || !c1 && !c2 && !c3 ) s = -s;  
            }
    
            return s * Mathf.Sqrt(d);
        }

        private void Update() {
            var mousePos = Input.mousePosition;
            var mousePosWorld = Camera.main.ScreenToWorldPoint(mousePos);
            _mousePos = new Vector2(mousePosWorld.x, mousePosWorld.y);
            _nearest = PointAlongPolygon(PolygonShapeData, _mousePos);

            var inv = Camera.main.worldToCameraMatrix.inverse;
            var inv2 = Camera.main.projectionMatrix.inverse;
            
            var mouseClip = new Vector4((mousePos.x * 2f / Screen.width) - 1f, (mousePos.y * 2f / Screen.height) - 1f, 0f, 1f);
            var mouseView = inv2.MultiplyPoint(mouseClip);
            //mouseView /= mouseView.w;
            
            var testPos = (Vector2)inv.MultiplyPoint(mouseView);

            Debug.Log($"{_mousePos}, {testPos}");
        }

        private void OnDrawGizmos() {
            foreach (var segment in _segments) {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(segment.A, Vector3.one * 0.2f);
                Gizmos.DrawWireCube(segment.B, Vector3.one * 0.2f);
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(segment.A, segment.B);
            }
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_mousePos, 0.2f);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(_mousePos, _nearest);

            foreach (var v in _output) {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(new Vector2(v.x, v.y), Vector3.one * 0.2f);
                Gizmos.DrawWireCube(new Vector2(v.z, v.w), Vector3.one * 0.2f);
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(new Vector2(v.x, v.y), new Vector2(v.z, v.w));
            }
        }
    }
}