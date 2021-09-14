using UnityEngine;
using VektorLighting2D.RayMarching.Lights;

namespace VektorLighting2D.Components.Lights {
    public class VektorPointLight : VektorLight {
        public PointLightData GetLightData() {
            return new PointLightData(transform.position, _color * _intensity, _range, enabled);
        }
        
        private void OnDrawGizmosSelected() {
            var origin = transform.position;
            var data = GetLightData();
            
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(origin, Vector3.one * 0.1f);
            Gizmos.color = _color;

            Gizmos.color = _color;
            Gizmos.DrawWireSphere(origin, _range);
        }
    }
}