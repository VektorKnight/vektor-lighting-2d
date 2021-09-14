using System.Collections.Generic;
using UnityEngine;

namespace VektorLighting2D {
    /// <summary>
    /// Triangulates an arbitrary polygon/contour without holes.
    /// Pretty much a 1:1 conversion from: https://www.flipcode.com/archives/Efficient_Polygon_Triangulation.shtml
    /// Some C#-specific optimizations to minimize garbage.
    /// </summary>
    public static class Triangulate {
        // Work buffers.
        private static readonly List<Vector2> _vertexBuffer;
        private static readonly int[] _triangleBuffer;
        

        static Triangulate() {
            _vertexBuffer = new List<Vector2>(256);
            _triangleBuffer = new int[1024];
        }

        /// <summary>
        /// Triangulate a contour/polygon.
        /// Results are placed into a provided list as a series of triangles.
        /// </summary>
        /// <param name="contour">The polygon/contour to process.</param>
        /// <param name="results">The list to which results will be written.</param>
        /// <returns></returns>
        public static bool Process(Vector2[] contour, List<Vector2> results) {
            /* allocate and initialize list of Vertices in polygon */

            var n = contour.Length;
            if ( n < 3 ) return false;

            /* we want a counter-clockwise polygon in V */

            if ( 0.0f < Area(contour) )
                for (int v=0; v<n; v++) _triangleBuffer[v] = v;
            else
                for(int v=0; v<n; v++) _triangleBuffer[v] = (n-1)-v;

            int nv = n;

            /*  remove nv-2 Vertices, creating 1 triangle every time */
            int count = 2*nv;   /* error detection */

            for(int m=0, v=nv-1; nv>2; )
            {
                /* if we loop, it is probably a non-simple polygon */
                if (0 >= (count--))
                {
                    //** Triangulate: ERROR - probable bad polygon!
                    return false;
                }

                /* three consecutive vertices in current polygon, <u,v,w> */
                var u = v  ; if (nv <= u) u = 0;     /* previous */
                v = u+1; if (nv <= v) v = 0;     /* new v    */
                var w = v+1; if (nv <= w) w = 0;     /* next     */

                if ( Snip(contour,u,v,w,nv, _triangleBuffer) )
                {
                    int s,t;

                    /* true names of the vertices */
                    var a = _triangleBuffer[u]; 
                    var b = _triangleBuffer[v]; 
                    var c = _triangleBuffer[w];

                    /* output Triangle */
                    results.Add(contour[a]);
                    results.Add(contour[b]);
                    results.Add(contour[c]);

                    m++;

                    /* remove v from remaining polygon */
                    for(s=v,t=v+1;t<nv;s++,t++) _triangleBuffer[s] = _triangleBuffer[t]; nv--;

                    /* reset error detection counter */
                    count = 2 * nv;
                }
            }

            return true;
        }
        
        /// <summary>
        /// Calculate the area of a contour/polygon.
        /// </summary>
        /// <returns>The area of the contour/polygon.</returns>
        public static float Area(Vector2[] contour) {
            var n = contour.Length;

            var a = 0f;

            for (int p = n - 1, q = 0; q < n; p = q++) {
                a += contour[p].x * contour[q].y - contour[q].x * contour[p].y;
            }

            return a * 0.5f;
        }
        
        /// <summary>
        /// Determine if a point lies within a triangle defined by (a, b, c).
        /// </summary>
        /// <returns>Whether or not the point is inside the given triangle.</returns>
        public static bool InsideTriangle(float Ax, float Ay,
                                          float Bx, float By,
                                          float Cx, float Cy,
                                          float Px, float Py)
        {
            var ax = Cx - Bx;   var ay = Cy - By;
            var bx = Ax - Cx;   var by = Ay - Cy;
            var cx = Bx - Ax;   var cy = By - Ay;
            var apx = Px - Ax;  var apy = Py - Ay;
            var bpx = Px - Bx;  var bpy = Py - By;
            var cpx = Px - Cx;  var cpy = Py - Cy;

            var aCrossBp = ax * bpy - ay * bpx;
            var cCrossAp = cx * apy - cy * apx;
            var bCrossCp = bx * cpy - by * cpx;

            return aCrossBp >= 0f && bCrossCp >= 0f && cCrossAp >= 0f;
        }
        
        // TODO: Come back to this.
        private static bool Snip(Vector2[] contour, int u, int v, int w, int n, int[] verts) {
            var Ax = contour[verts[u]].x;
            var Ay = contour[verts[u]].y;

            var Bx = contour[verts[v]].x;
            var By = contour[verts[v]].y;

            var Cx = contour[verts[w]].x;
            var Cy = contour[verts[w]].y;

            if (float.Epsilon > (Bx - Ax) * (Cy - Ay) - (By - Ay) * (Cx - Ax)) return false;

            for (var p = 0; p < n; p++) {
                if ((p == u) || (p == v) || (p == w)) continue;
                var Px = contour[verts[p]].x;
                var Py = contour[verts[p]].y;
                if (InsideTriangle(Ax, Ay, Bx, By, Cx, Cy, Px, Py)) return false;
            }

            return true;
        }
    }
}