using System;
using Unity.VisualScripting;
using UnityEngine;
using VektorLighting2D.RayMarching.Lights;

namespace VektorLighting2D.Components.Lights {
    public class VektorSpotLight : VektorLight {
        [SerializeField] [Range(1f, 179f)] private float _angle = 60f;
        public SpotLightData GetLightData() {
            return new SpotLightData(transform.position, _color * _intensity, _range, 179f - _angle, transform.right, enabled);
        }

        private void OnDrawGizmosSelected() {
            var origin = transform.position;
            var data = GetLightData();
            
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(origin, Vector3.one * 0.1f);
            Gizmos.color = _color;
            
            Gizmos.DrawLine(origin, origin + (Vector3)(Vector3.Reflect(-data.ConeMin, transform.right)) * _range);
            Gizmos.DrawLine(origin, origin + (Vector3)(Vector3.Reflect(-data.ConeMax, transform.right)) * _range);
            
            Gizmos.color = Color.grey;
            Gizmos.DrawWireSphere(origin, _range);
        }
    }
}