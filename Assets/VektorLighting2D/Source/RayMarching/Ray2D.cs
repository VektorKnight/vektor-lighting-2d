using System.Runtime.InteropServices;
using UnityEngine;

namespace VektorLighting2D.RayMarching {
    [StructLayout(LayoutKind.Sequential)]
    public struct Ray2D {
        public uint id;
        public Vector2 Origin;
        public Vector2 Direction;
        public Vector3 Color;
        //public float Distance;

        //public uint LightId;
        //public uint LightType;
        public float LightDistance;
    }
}