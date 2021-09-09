using System;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace VektorLighting2D.RayMarching {
    public class RayMarchTest : MonoBehaviour {
        [SerializeField] private float _renderScale = 0.25f;
        [SerializeField] private int _maxSteps = 32;
        [SerializeField] private int _maxBounces = 8;
        [SerializeField] private ComputeShader _rayMarch;
        [SerializeField] private RawImage _target;

        private PointLight[] _pointLights;
        private Circle[] _circles;
        private Rect[] _rects;
        
        private Vector2Int _dimensions;

        private ComputeBuffer _lightBuffer;
        
        private ComputeBuffer _circleBuffer;
        private ComputeBuffer _rectBuffer;
        
        private ComputeBuffer _rayBuffer;
        private RenderTexture _resultTexture;
        
        private static readonly int _idMaxSteps = Shader.PropertyToID("MaxSteps");
        private static readonly int _idMaxBounces = Shader.PropertyToID("MaxBounces");
        private static readonly int _idLightCount = Shader.PropertyToID("LightCount");
        
        private static readonly int _idWidth = Shader.PropertyToID("Width");
        private static readonly int _idHeight = Shader.PropertyToID("Height");

        private static readonly int _idPointLights = Shader.PropertyToID("PointLights");
        
        private static readonly int _idCircles = Shader.PropertyToID("Circles");
        private static readonly int _idRects = Shader.PropertyToID("Rects");
        
        private static readonly int _idRays = Shader.PropertyToID("Rays");
        private static readonly int _idResult = Shader.PropertyToID("Result");

        private void Start() {
            _dimensions = new Vector2Int((int)(Screen.width * _renderScale), (int)(Screen.height * _renderScale));

            _pointLights = new[] {
                new PointLight(Vector2.zero, Vector3.one, 100f * _renderScale),
                
                new PointLight(new Vector3(0.1f * _dimensions.x, 0.1f * _dimensions.y), new Vector3(1f, 0f, 0f), 768f * _renderScale),
                new PointLight(new Vector3(0.1f * _dimensions.x, 0.9f * _dimensions.y), new Vector3(0f, 1f, 0f), 768f * _renderScale),
                new PointLight(new Vector3(0.9f * _dimensions.x, 0.9f * _dimensions.y), new Vector3(0f, 0f, 1f), 768f * _renderScale),
                new PointLight(new Vector3(0.9f * _dimensions.x, 0.1f * _dimensions.y), new Vector3(1f, 0f, 1f), 768f * _renderScale)
            };
            
            _circles = new[] {
                new Circle(new Vector2(0.25f * _dimensions.x, 0.25f * _dimensions.y), 16f * _renderScale),
                new Circle(new Vector2(0.25f * _dimensions.x, 0.75f * _dimensions.y), 16f * _renderScale),
                new Circle(new Vector2(0.75f * _dimensions.x, 0.75f * _dimensions.y), 16f * _renderScale),
                new Circle(new Vector2(0.75f * _dimensions.x, 0.25f * _dimensions.y), 16f * _renderScale),
            };

            _rects = new[] {
                new Rect(new Vector2(0.5f * _dimensions.x, 0.5f * _dimensions.y), new Vector2(16f, 8f) * _renderScale),
            };

            _lightBuffer = new ComputeBuffer(_pointLights.Length, Marshal.SizeOf<PointLight>());
            
            _circleBuffer = new ComputeBuffer(_circles.Length, Marshal.SizeOf<Circle>());
            _rectBuffer = new ComputeBuffer(_rects.Length, Marshal.SizeOf<Rect>());
            
            _lightBuffer.SetData(_pointLights);
            _circleBuffer.SetData(_circles);
            _rectBuffer.SetData(_rects);
            
            _rayBuffer = new ComputeBuffer(_dimensions.x * _dimensions.y * _pointLights.Length, Marshal.SizeOf<Ray2D>());
            _resultTexture = new RenderTexture(_dimensions.x, _dimensions.y, 1, RenderTextureFormat.Default) {
                enableRandomWrite = true,
                filterMode = FilterMode.Point
            };

            _rayMarch.SetInt(_idMaxSteps, _maxSteps);
            _rayMarch.SetInt(_idMaxBounces, _maxBounces);
            _rayMarch.SetInt(_idLightCount, _pointLights.Length);

            _rayMarch.SetInt(_idWidth, _dimensions.x);
            _rayMarch.SetInt(_idHeight, _dimensions.y);
            
            _rayMarch.SetBuffer(0, _idPointLights, _lightBuffer);
            _rayMarch.SetBuffer(0, _idRects, _rectBuffer);
            _rayMarch.SetBuffer(0, _idCircles, _circleBuffer);
            _rayMarch.SetBuffer(0, _idRays, _rayBuffer);
            _rayMarch.SetTexture(0, _idResult, _resultTexture);

            _rayMarch.SetBuffer(1, _idPointLights, _lightBuffer);
            _rayMarch.SetBuffer(1, _idRects, _rectBuffer);
            _rayMarch.SetBuffer(1, _idCircles, _circleBuffer);
            _rayMarch.SetBuffer(1, _idRays, _rayBuffer);
            
            _rayMarch.SetBuffer(2, _idRays, _rayBuffer);
            _rayMarch.SetTexture(2, _idResult, _resultTexture);

            _target.texture = _resultTexture;
        }

        private void Update() {
            var mouseNorm = new Vector2(Input.mousePosition.x * _renderScale, Input.mousePosition.y * _renderScale);
            _pointLights[0] = new PointLight(mouseNorm, new Vector3(1f, 1f, 1f), 256f * _renderScale);
            
            _lightBuffer.SetData(_pointLights);
            
            _rayMarch.Dispatch(0, Mathf.CeilToInt((float)_dimensions.x / 8), Mathf.CeilToInt((float)_dimensions.y / 8), 1);
            _rayMarch.Dispatch(1, Mathf.CeilToInt((float)(_dimensions.x * _dimensions.y * _pointLights.Length) / 512), 1, 1);
            _rayMarch.Dispatch(2, Mathf.CeilToInt((float)_dimensions.x / 8), Mathf.CeilToInt((float)_dimensions.y / 8), 1);
        }

        private void OnDestroy() {
            _lightBuffer.Dispose();
            _rectBuffer.Dispose();
            _circleBuffer.Dispose();
            _rayBuffer.Dispose();
            _resultTexture.Release();
        }
    }
}