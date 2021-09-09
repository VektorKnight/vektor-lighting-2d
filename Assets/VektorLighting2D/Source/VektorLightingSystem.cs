using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VektorLighting2D {
    public sealed class VektorLightingSystem : MonoBehaviour {
        public static VektorLightingSystem Instance { get; private set; }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeSingleton() {
            if (Instance != null) {
                Debug.LogWarning("[Vektor Lighting System]: Duplicate lighting system detected.");
                return;
            }

            Instance = new GameObject("[Vektor Lighting System]").AddComponent<VektorLightingSystem>();
            Instance.Initialize();
        }

        private static readonly int _idMainTex = Shader.PropertyToID("_MainTex");
        private static readonly int _idAlphaCutoff = Shader.PropertyToID("_AlphaCutoff");

        private Camera _camera;
        private Material _shadowMaterial;
        private RenderTexture _occlusionMap;
        private CommandBuffer _commandBuffer;
        
        private List<VektorShadowCaster> _shadowCasters;
        private bool _initialized;

        public RenderTexture OcclusionMap => _occlusionMap;

        public void AddShadowCaster(VektorShadowCaster caster) {
            if (caster == null) {
                throw new ArgumentNullException(nameof(caster));
            }

            if (_shadowCasters.Contains(caster)) {
                throw new ArgumentException("Shadow caster has already been added.", nameof(caster));
            }
            
            _shadowCasters.Add(caster);
        }

        public void RemoveShadowCaster(VektorShadowCaster caster) {
            if (caster == null) {
                throw new ArgumentNullException(nameof(caster));
            }

            if (!_shadowCasters.Contains(caster)) {
                throw new ArgumentException("Shadow caster has not been added.", nameof(caster));
            }
            
            _shadowCasters.Remove(caster);
        }
        
        private void Initialize() {
            if (_initialized) {
                Debug.LogWarning("[Vektor Lighting System]: Initialization called more than once!");
                return;
            }

            _shadowCasters = new List<VektorShadowCaster>();

            _camera = Camera.main;
            _shadowMaterial = new Material(Shader.Find("Vektor/Lighting2D/VektorShadowCaster2D"));
            _occlusionMap = new RenderTexture(Screen.width / 2, Screen.height / 2, 1, RenderTextureFormat.Default);
            _commandBuffer = new CommandBuffer();
            
            _camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, _commandBuffer);
            
            _initialized = true;
        }

        private void Update() {
            if (!_initialized) return;
            
            // Build the command buffer.
            _commandBuffer.Clear();
            _commandBuffer.SetRenderTarget(_occlusionMap);
            _commandBuffer.ClearRenderTarget(true, true, Color.white);

            foreach (var caster in _shadowCasters) {
                _commandBuffer.DrawRenderer(caster.Renderer, _shadowMaterial, 0, -1);
            }
        }

        private void OnDestroy() {
            _occlusionMap.Release();
            _commandBuffer.Release();
        }
    }
}