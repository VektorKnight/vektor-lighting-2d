using System;
using UnityEngine;

namespace VektorLighting2D {
    [RequireComponent(typeof(Renderer))]
    public class VektorShadowCaster : MonoBehaviour {
        [SerializeField] private float _alphaCutoff = 0.1f;

        public RenderTexture OcclusionMap;
        
        private bool _enabled;
        private Renderer _renderer;
        private Texture2D _texture;

        public bool Enabled => _enabled;
        public Renderer Renderer => _renderer;
        public Texture2D Texture => _texture;
        public float AlphaCutoff => _alphaCutoff;

        private void Start() {
            _renderer = GetComponent<Renderer>();
            _texture = Renderer.material.mainTexture as Texture2D;
            
            VektorLightingSystem.Instance.AddShadowCaster(this);
            OcclusionMap = VektorLightingSystem.Instance.OcclusionMap;
        }

        private void OnEnable() {
            _enabled = true;
        }

        private void OnDisable() {
            _enabled = false;
        }

        private void OnDestroy() {
            VektorLightingSystem.Instance.RemoveShadowCaster(this);
        }
    }
}
