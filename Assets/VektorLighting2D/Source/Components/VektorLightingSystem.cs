using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VektorLighting2D.Components.Lights;
using VektorLighting2D.Components.Shapes;
using VektorLighting2D.RayMarching;
using VektorLighting2D.RayMarching.Lights;
using VektorLighting2D.RayMarching.Shapes;

namespace VektorLighting2D.Components {
    [RequireComponent(typeof(Camera))]
    public class VektorLightingSystem : MonoBehaviour {
        // Unity settings.
        [Header("Ray-Marching Config")]
        [SerializeField] private float _renderScale = 1f;
        [SerializeField] private int _maxSteps = 64;
        [SerializeField] private ComputeShader _rayMarch;
        [SerializeField] private RawImage _target;
        
        // Lights and occluders.
        private static readonly List<VektorLight> _lights;
        private static readonly List<VektorShape> _shapes;
        
        private static bool _lightsDirty = true;
        private static bool _shapesDirty = true;
        
        // GPU staging buffers.
        private static readonly List<PointLightData> _pointLights;
        private static readonly List<SpotLightData> _spotLights;
        private static readonly List<PolygonLightData> _polygonLights;
        private static readonly List<Segment> _lightSegments;

        private static readonly List<CircleShapeData> _circleShapes;
        private static readonly List<RectShapeData> _rectShapes;
        private static readonly List<PolygonShapeData> _polygonShapes;
        private static readonly List<Segment> _shapeSegments;
        
        // Renderer and camera instances.
        private Camera _camera;
        private RayMarchRenderer _renderer;

        static VektorLightingSystem() {
            _lights = new List<VektorLight>();
            _shapes = new List<VektorShape>();

            _pointLights = new List<PointLightData>();
            _spotLights = new List<SpotLightData>();
            _polygonLights = new List<PolygonLightData>();
            _lightSegments = new List<Segment>();

            _circleShapes = new List<CircleShapeData>();
            _rectShapes = new List<RectShapeData>();
            _polygonShapes = new List<PolygonShapeData>();
            _shapeSegments = new List<Segment>();
        }

        private static void RebuildLightBuffers() {
            // Light buffers.
            _pointLights.Clear();
            _spotLights.Clear();
            _polygonLights.Clear();
            _lightSegments.Clear();
            foreach (var light in _lights) {
                switch (light) {
                    case VektorPointLight pointLight:
                        _pointLights.Add(pointLight.GetLightData());
                        break;
                    case VektorSpotLight spotLight:
                        _spotLights.Add(spotLight.GetLightData());
                        break;
                    case VektorPolygonLight polygonLight:
                        _polygonLights.Add(polygonLight.GetLightData(_lightSegments));
                        break;
                    default:
                        Debug.LogWarning("Skipping unsupported light type.");
                        break;
                }
            }

            _lightsDirty = false;
        }

        private static void RebuildShapeBuffers() {
            // Shape buffers.
            _circleShapes.Clear();
            _rectShapes.Clear();
            _polygonShapes.Clear();
            _shapeSegments.Clear();
            foreach (var shape in _shapes) {
                switch (shape) {
                    case VektorCircleShape circleShape:
                        _circleShapes.Add(circleShape.GetShapeData());
                        break;
                    case VektorRectShape rectShape:
                        _rectShapes.Add(rectShape.GetShapeData());
                        break;
                    case VektorPolygonShape polygonShape:
                        _polygonShapes.Add(polygonShape.GetPolygonData(_shapeSegments));
                        break;
                    default:
                        Debug.LogWarning("Skipping unsupported shape type.");
                        break;
                }
            }

            _shapesDirty = false;
        }

        private void Awake() {
            _camera = GetComponent<Camera>();
            _renderer = new RayMarchRenderer(_rayMarch, _camera, _maxSteps, 1, _renderScale);
            _target.texture = _renderer.ResultTexture;
        }

        private void Update() {
            RebuildLightBuffers();
            RebuildShapeBuffers();
                
            _renderer.UpdateLightBuffers(_pointLights, _spotLights, _polygonLights, _lightSegments);
            _renderer.UpdateShapeBuffers(_circleShapes, _rectShapes, _polygonShapes, _shapeSegments);
            
            _renderer.Render();
        }

        private void OnDestroy() {
            _renderer.Dispose();
        }

        public static void AddLight(VektorLight light) {
            if (light == null) {
                throw new ArgumentNullException(nameof(light));
            }
            
            if (_lights.Contains(light)) {
                throw new Exception("Light has already been added.");
            }
            
            _lights.Add(light);
            _lightsDirty = true;
        }

        public static void RemoveLight(VektorLight light) {
            if (light == null) {
                throw new ArgumentNullException(nameof(light));
            }
            
            if (!_lights.Contains(light)) {
                throw new Exception("Light has not been added.");
            }

            _lights.Remove(light);
            _lightsDirty = true;
        }
        
        public static void AddShape(VektorShape shape) {
            if (shape == null) {
                throw new ArgumentNullException(nameof(shape));
            }
            
            if (_shapes.Contains(shape)) {
                throw new Exception("Occluder has already been added.");
            }
            
            _shapes.Add(shape);
            _shapesDirty = true;
        }

        public static void RemoveShape(VektorShape shape) {
            if (shape == null) {
                throw new ArgumentNullException(nameof(shape));
            }
            
            if (!_shapes.Contains(shape)) {
                throw new Exception("Light has not been added.");
            }

            _shapes.Remove(shape);
            _shapesDirty = true;
        }
    }
}