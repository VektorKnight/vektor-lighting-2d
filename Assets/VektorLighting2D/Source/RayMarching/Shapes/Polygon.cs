using System.Runtime.InteropServices;
using UnityEngine;

namespace VektorLighting2D.RayMarching.Shapes {
    [StructLayout(LayoutKind.Sequential)]
    public struct Polygon {
        public uint Offset;
        public uint Length;

        public Polygon(uint offset, uint length) {
            Offset = offset;
            Length = length;
        }
    }
}