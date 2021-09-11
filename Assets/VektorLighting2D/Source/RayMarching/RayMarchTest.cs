using System;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using VektorLighting2D.RayMarching.Lights;
using VektorLighting2D.RayMarching.Shapes;
using Random = UnityEngine.Random;
using Rect = VektorLighting2D.RayMarching.Shapes.Rect;

namespace VektorLighting2D.RayMarching {
    public class RayMarchTest : MonoBehaviour {
        [SerializeField] private float _renderScale = 0.25f;
        [SerializeField] private int _maxSteps = 32;
        [SerializeField] private int _maxBounces = 8;
        [SerializeField] private ComputeShader _rayMarch;
        [SerializeField] private RawImage _target;

        private PointLight[] _pointLights;
        private SpotLight[] _spotLights;
        private PolygonLight[] _polygonLights;
        
        private Circle[] _circles;
        private Rect[] _rects;
        private Segment[] _segments;
        
        private Vector2Int _dimensions;

        private int _rayCountMax;

        private ComputeBuffer _pointLightBuffer;
        private ComputeBuffer _spotLightBuffer;
        private ComputeBuffer _polygonLightBuffer;
        
        private ComputeBuffer _circleBuffer;
        private ComputeBuffer _rectBuffer;
        private ComputeBuffer _segmentBuffer;

        private ComputeBuffer _rayBufferA;
        private ComputeBuffer _rayBufferB;

        private ComputeBuffer _accumulationBuffer;
        private RenderTexture _resultTexture;

        private CommandBuffer _commandBuffer;

        private static readonly int _idMaxSteps = Shader.PropertyToID("MaxSteps");
        private static readonly int _idMaxBounces = Shader.PropertyToID("MaxBounces");

        private static readonly int _idWidth = Shader.PropertyToID("Width");
        private static readonly int _idHeight = Shader.PropertyToID("Height");

        private static readonly int _idPointLights = Shader.PropertyToID("PointLights");
        private static readonly int _idSpotLights = Shader.PropertyToID("SpotLights");
        private static readonly int _idPolygonLights = Shader.PropertyToID("PolygonLights");
        
        private static readonly int _idCircles = Shader.PropertyToID("Circles");
        private static readonly int _idRects = Shader.PropertyToID("Rects");
        private static readonly int _idSegments = Shader.PropertyToID("Segments");
        
        private static readonly int _idInputRays = Shader.PropertyToID("InputRays");
        private static readonly int _idOutputRays = Shader.PropertyToID("OutputRays");

        private static readonly int _idAccumulation = Shader.PropertyToID("Accumulation");
        private static readonly int _idResult = Shader.PropertyToID("Result");

        private void Start() {
            _dimensions = new Vector2Int((int)(Screen.width * _renderScale), (int)(Screen.height * _renderScale));

            _pointLights = new[] {
                //new PointLight(Vector2.zero, Vector3.one, 100f * _renderScale),
                
                new PointLight(new Vector3(0.1f * _dimensions.x, 0.1f * _dimensions.y), new Vector3(1f, 0f, 0f), 512f * _renderScale),
                new PointLight(new Vector3(0.1f * _dimensions.x, 0.9f * _dimensions.y), new Vector3(0f, 1f, 0f), 512f * _renderScale),
                new PointLight(new Vector3(0.9f * _dimensions.x, 0.9f * _dimensions.y), new Vector3(0f, 0f, 1f), 512f * _renderScale),
                new PointLight(new Vector3(0.9f * _dimensions.x, 0.1f * _dimensions.y), new Vector3(1f, 0f, 1f), 512f * _renderScale)
            };

            _spotLights = new[] {
                new SpotLight(new Vector2(0.5f * _dimensions.x, 0.25f * _dimensions.y), new Vector3(1, 0, 1), 1024f, 90f, Vector2.down),
                new SpotLight(new Vector2(0.5f * _dimensions.x, 0.75f * _dimensions.y), new Vector3(0, 1, 1), 1024f, 90f, Vector2.up)
            };

            _polygonLights = new[] {
                new PolygonLight(new Vector3(1f, 1f, 0.5f), 512f, new Polygon(0, 3))
            };

            _segments = new[] {
                new Segment(
                    new Vector2(0.25f * _dimensions.x, 0.25f * _dimensions.y), 
                    new Vector2(0.5f * _dimensions.x, 0.75f * _dimensions.y)
                ),
                new Segment(
                    new Vector2(0.5f * _dimensions.x, 0.75f * _dimensions.y),
                    new Vector2(0.75f * _dimensions.x, 0.25f * _dimensions.y)
                ),
                new Segment(
                    new Vector2(0.75f * _dimensions.x, 0.25f * _dimensions.y),
                    new Vector2(0.25f * _dimensions.x, 0.25f * _dimensions.y)
                ),
            };
            
            _circles = new[] {
                new Circle(new Vector2(0.4f * _dimensions.x, 0.4f * _dimensions.y), 16f * _renderScale),
                new Circle(new Vector2(0.25f * _dimensions.x, 0.75f * _dimensions.y), 16f * _renderScale),
                new Circle(new Vector2(0.75f * _dimensions.x, 0.75f * _dimensions.y), 16f * _renderScale)
            };

            _rects = new[] {
                new Rect(new Vector2(0.5f * _dimensions.x, -0.4f * _dimensions.y), new Vector2(512f, 8f) * _renderScale),
                //new Rect(new Vector2(0.5f * _dimensions.x, 2f * _dimensions.y), new Vector2(512f, 8f) * _renderScale),
            };

            _pointLightBuffer = new ComputeBuffer(_pointLights.Length, Marshal.SizeOf<PointLight>());
            _spotLightBuffer = new ComputeBuffer(_spotLights.Length, Marshal.SizeOf<SpotLight>());
            _polygonLightBuffer = new ComputeBuffer(_polygonLights.Length, Marshal.SizeOf<PolygonLight>());
            
            _circleBuffer = new ComputeBuffer(_circles.Length, Marshal.SizeOf<Circle>());
            _rectBuffer = new ComputeBuffer(_rects.Length, Marshal.SizeOf<Rect>());
            _segmentBuffer = new ComputeBuffer(_segments.Length, Marshal.SizeOf<Segment>());

            _pointLightBuffer.SetData(_pointLights);
            _spotLightBuffer.SetData(_spotLights);
            _polygonLightBuffer.SetData(_polygonLights);
            
            _circleBuffer.SetData(_circles);
            _rectBuffer.SetData(_rects);
            _segmentBuffer.SetData(_segments);

            var n = _dimensions.x * _dimensions.y * (_pointLights.Length + _spotLights.Length + (_polygonLights.Length));
            _rayCountMax = n;
                
            _rayBufferA = new ComputeBuffer(_rayCountMax, Marshal.SizeOf<Ray2D>(), ComputeBufferType.Append);
            _rayBufferB = new ComputeBuffer(_rayCountMax, Marshal.SizeOf<Ray2D>(), ComputeBufferType.Append);

            _accumulationBuffer = new ComputeBuffer(_dimensions.x * _dimensions.y, 4, ComputeBufferType.Structured);

            _resultTexture = new RenderTexture(_dimensions.x, _dimensions.y, 1, RenderTextureFormat.Default) {
                enableRandomWrite = true,
                filterMode = FilterMode.Point
            };

            _rayMarch.SetInt(_idMaxSteps, _maxSteps);
            _rayMarch.SetInt(_idMaxBounces, _maxBounces);

            _rayMarch.SetInt(_idWidth, _dimensions.x);
            _rayMarch.SetInt(_idHeight, _dimensions.y);
            
            _rayMarch.SetBuffer(0, _idPointLights, _pointLightBuffer);
            _rayMarch.SetBuffer(0, _idSpotLights, _spotLightBuffer);
            _rayMarch.SetBuffer(0, _idPolygonLights, _polygonLightBuffer);
            _rayMarch.SetBuffer(0, _idRects, _rectBuffer);
            _rayMarch.SetBuffer(0, _idCircles, _circleBuffer);
            _rayMarch.SetBuffer(0, _idSegments, _segmentBuffer);
            _rayMarch.SetBuffer(0, _idOutputRays, _rayBufferB);
            _rayMarch.SetBuffer(0, _idAccumulation, _accumulationBuffer);
            _rayMarch.SetTexture(0, _idResult, _resultTexture);

            _rayMarch.SetBuffer(1, _idPointLights, _pointLightBuffer);
            _rayMarch.SetBuffer(1, _idRects, _rectBuffer);
            _rayMarch.SetBuffer(1, _idCircles, _circleBuffer);
            _rayMarch.SetBuffer(1, _idAccumulation, _accumulationBuffer);
            
            _rayMarch.SetBuffer(2, _idAccumulation, _accumulationBuffer);
            _rayMarch.SetTexture(2, _idResult, _resultTexture);

            _target.texture = _resultTexture;

            _commandBuffer = new CommandBuffer();
        }

        private void Update() {
            var mouseNorm = new Vector2(Input.mousePosition.x * _renderScale, Input.mousePosition.y * _renderScale);
            _circles[0] = new Circle(mouseNorm, 32f * _renderScale);
            _spotLights[0] = new SpotLight(new Vector2(0.5f * _dimensions.x, 0.8f * _dimensions.y), new Vector3(1, 0, 1), 2048f, ((Mathf.Sin(Time.time) + 1.0f) * 0.5f) * 179f, Vector2.down);
            _spotLights[1] = new SpotLight(new Vector2(0.5f * _dimensions.x, 0.2f * _dimensions.y), new Vector3(0, 1, 1), 2048f, ((Mathf.Sin(Time.time) + 1.0f) * 0.5f) * 179f, Vector2.up);
            
            _pointLightBuffer.SetData(_pointLights);
            _spotLightBuffer.SetData(_spotLights);
            _circleBuffer.SetData(_circles);

            // Clear append buffers.
            _rayBufferA.SetCounterValue(0);
            _rayBufferB.SetCounterValue(0);
            
            // Generate initial rays.
            _rayMarch.Dispatch(0, Mathf.CeilToInt((float)_dimensions.x / 8), Mathf.CeilToInt((float)_dimensions.y / 8), 1);

            // Re-run the march kernel for each bounce.
            for (var b = 0; b < _maxBounces; b++) {
                // Flip buffers so the output rays of the previous stage become the new input rays.
                if (b % 2 == 0) {
                    _rayMarch.SetBuffer(1, _idInputRays, _rayBufferB);
                    _rayMarch.SetBuffer(1, _idOutputRays, _rayBufferA);
                }
                else {
                    _rayMarch.SetBuffer(1, _idInputRays, _rayBufferA);
                    _rayMarch.SetBuffer(1, _idOutputRays, _rayBufferB);
                }
                
                // Dispatch as any times as necessary to consume the max possible rays.
                var groupCount = Mathf.CeilToInt((float)_rayCountMax / 64);
                var dispatchCount = Mathf.CeilToInt((float)groupCount / 65535);
                for (var i = 0; i < dispatchCount; i++) {
                    _rayMarch.Dispatch(1, 65535, 1, 1);
                }
            }

            // Generate output texture.
            _rayMarch.Dispatch(2, Mathf.CeilToInt((float)_dimensions.x / 8), Mathf.CeilToInt((float)_dimensions.y / 8), 1);
        }

        private void OnDestroy() {
            _pointLightBuffer.Dispose();
            _pointLightBuffer.Dispose();
            _rectBuffer.Dispose();
            _circleBuffer.Dispose();
            _segmentBuffer.Dispose();
            _rayBufferA.Dispose();
            _rayBufferB.Dispose();
            _accumulationBuffer.Dispose();
            _resultTexture.Release();
            _commandBuffer.Release();
        }
    }
}