using System.Collections.Generic;
using UnityEngine;
using VektorLighting2D.Components.Shapes;
using VektorLighting2D.RayMarching.Lights;
using VektorLighting2D.RayMarching.Shapes;

namespace VektorLighting2D.Components.Lights {
    [RequireComponent(typeof(VektorPolygonShape))]
    public class VektorPolygonLight : VektorLight {
        private VektorPolygonShape _polygonShape;

        protected override void Awake() {
            _polygonShape = GetComponent<VektorPolygonShape>();
            base.Awake();
        }

        public PolygonLightData GetLightData(in List<Segment> segmentBuffer) {
            var shapeData = _polygonShape.GetPolygonData(segmentBuffer);
            return new PolygonLightData(_color, _range, shapeData, enabled);
        }
    }
}