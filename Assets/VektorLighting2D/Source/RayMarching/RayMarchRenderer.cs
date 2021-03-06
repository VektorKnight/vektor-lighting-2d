using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;
using UnityEngine.UI;
using VektorLighting2D.RayMarching.Lights;
using VektorLighting2D.RayMarching.Shapes;
using Random = UnityEngine.Random;

namespace VektorLighting2D.RayMarching {
    public class RayMarchRenderer : IDisposable {
        private const int KERNEL_ID_INITIALIZE = 0;     // Generates initial rays from the scene and enabled lights.
        private const int KERNEL_ID_MARCH = 1;          // Marches generated rays and may generate secondary rays.
        private const int KERNEL_ID_FINALIZE = 2;       // Renders the final result to the output texture.

        private const int DISPATCH_GROUP_SIZE = 64;
        private const int DISPATCH_GROUP_COUNT = 65535;
        
        // General ray-marching config.
        private static readonly int _idMaxSteps = Shader.PropertyToID("MaxSteps");
        private static readonly int _idMaxBounces = Shader.PropertyToID("MaxBounces");

        private static readonly int _idPixelsPerBatch = Shader.PropertyToID("PixelsPerBatch");
        private static readonly int _idBatchOffset = Shader.PropertyToID("BatchOffset");
        private static readonly int _idFrameEven = Shader.PropertyToID("FrameEven");

        // Screen params.
        private static readonly int _idWidth = Shader.PropertyToID("Width");
        private static readonly int _idHeight = Shader.PropertyToID("Height");
        private static readonly int _idInverseProjection = Shader.PropertyToID("InverseProjection");
        private static readonly int _idInverseWorld = Shader.PropertyToID("InverseWorld");
        private static readonly int _idRenderScale = Shader.PropertyToID("RenderScale");
        private static readonly int _idPixelSize = Shader.PropertyToID("PixelSize");
        
        // Light data buffers.
        private static readonly int _idPointLights = Shader.PropertyToID("PointLights");
        private static readonly int _idSpotLights = Shader.PropertyToID("SpotLights");
        private static readonly int _idPolygonLights = Shader.PropertyToID("PolygonLights");
        private static readonly int _idLightSegments = Shader.PropertyToID("LightSegments");
        
        // Shape data buffers.
        private static readonly int _idCircleShapes = Shader.PropertyToID("CircleShapes");
        private static readonly int _idRectShapes = Shader.PropertyToID("RectShapes");
        private static readonly int _idPolygonShapes = Shader.PropertyToID("PolygonShapes");
        private static readonly int _idShapeSegments = Shader.PropertyToID("ShapeSegments");
        
        // Ray buffers.
        private static readonly int _idInputRays = Shader.PropertyToID("InputRays");
        private static readonly int _idOutputRays = Shader.PropertyToID("OutputRays");
        private static readonly int _idRayCounter = Shader.PropertyToID("RayCounter");

        // Result buffers.
        private static readonly int _idAccumulation = Shader.PropertyToID("Accumulation");
        private static readonly int _idResult = Shader.PropertyToID("Result");
        
        private static readonly int _idVektorLightMap = Shader.PropertyToID("_VektorLightMap");
        
        private readonly ComputeShader _rayMarch;
        private readonly Camera _camera;

        private readonly int _maxBounces;

        private int _lightCount;
        private int _shapeCount;
        private int _rayCountMax;
        private int _rayBatchCount;
        private int _pixelsPerBatch;

        private ComputeBuffer _pointLightBuffer;
        private ComputeBuffer _spotLightBuffer;
        private ComputeBuffer _polygonLightBuffer;
        private ComputeBuffer _lightSegmentBuffer;
        
        private ComputeBuffer _circleBuffer;
        private ComputeBuffer _rectBuffer;
        private ComputeBuffer _polygonBuffer;
        private ComputeBuffer _shapeSegmentBuffer;

        private readonly ComputeBuffer _rayBuffer;
        private readonly ComputeBuffer _rayCounter;

        private readonly ComputeBuffer _accumulationBuffer;
        
        private readonly CommandBuffer _commandBuffer;
        private readonly RenderTexture _resultTexture;

        public RenderTexture ResultTexture => _resultTexture;

        public RayMarchRenderer(ComputeShader rayMarch, Camera camera, int maxSteps, int maxBounces, float renderScale) {
            _rayMarch = rayMarch;
            _camera = camera;
            _maxBounces = maxBounces;
            
            _commandBuffer = new CommandBuffer();
            
            // Create the result texture based on the render scale.
            // Random write is so the shader can write to it directly.
            // We take advantage of bilinear filtering to reduce scaling artifacts.
            _resultTexture = _resultTexture = new RenderTexture((int)(Screen.width * renderScale), (int)(Screen.height * renderScale), 1) {
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear
            };
            
            // The ray buffer is allocated to be the max possible rays per batch.
            // A secondary buffer is used to hold an atomic counter to make sure we don't underflow the ray buffer.
            _rayBuffer = new ComputeBuffer(DISPATCH_GROUP_SIZE * DISPATCH_GROUP_COUNT, Marshal.SizeOf<Ray2D>(), ComputeBufferType.Append);
            _rayCounter = new ComputeBuffer(1, 4, ComputeBufferType.IndirectArguments);
            
            // An accumulation buffer is used to store the contributions of all the rays before being applied to the result.
            // This is necessary as atomic ops only work on 32-bit int types and multiple rays may right to the same pixel.
            _accumulationBuffer = new ComputeBuffer(_resultTexture.width * _resultTexture.height, 4, ComputeBufferType.Structured);
            
            _rayMarch.SetInt(_idMaxSteps, maxSteps);
            _rayMarch.SetInt(_idMaxBounces, _maxBounces);

            _rayMarch.SetInt(_idWidth, _resultTexture.width);
            _rayMarch.SetInt(_idHeight, _resultTexture.height);
            
            // This is used by the shader to rescale pixel coordinates to match world space regardless of the render scale.
            var renderScaleFactor = Mathf.Sqrt(1f / renderScale);
            _rayMarch.SetFloat(_idRenderScale, renderScaleFactor);
        }

        public void UpdateLightBuffers(List<PointLightData> pointLights, List<SpotLightData> spotLights, List<PolygonLightData> polyLights, List<Segment> lightSegments) {
            WriteBufferData(pointLights, ref _pointLightBuffer);
            WriteBufferData(spotLights, ref _spotLightBuffer);
            WriteBufferData(polyLights, ref _polygonLightBuffer);
            WriteBufferData(lightSegments, ref _lightSegmentBuffer);

            _lightCount = pointLights.Count + spotLights.Count + polyLights.Count;
            _rayCountMax = _resultTexture.width * _resultTexture.height * _lightCount / 2;

            _rayBatchCount = Mathf.CeilToInt((float)((double)_rayCountMax / (DISPATCH_GROUP_SIZE * DISPATCH_GROUP_COUNT)));
            _pixelsPerBatch = Mathf.FloorToInt((float)(DISPATCH_GROUP_SIZE * DISPATCH_GROUP_COUNT) / _lightCount);
            
            Debug.Log($"T:{_pixelsPerBatch * _rayBatchCount} | A:{_resultTexture.width * _resultTexture.height} | B:{_pixelsPerBatch} | C:{_rayBatchCount}");
        }

        public void UpdateShapeBuffers(List<CircleShapeData> circleShapes, List<RectShapeData> rectShapes, List<PolygonShapeData> polygonShapes, List<Segment> shapeSegments) {
            WriteBufferData(circleShapes, ref _circleBuffer);
            WriteBufferData(rectShapes, ref _rectBuffer);
            WriteBufferData(polygonShapes, ref _polygonBuffer);
            WriteBufferData(shapeSegments, ref _shapeSegmentBuffer);

            _shapeCount = circleShapes.Count + rectShapes.Count + polygonShapes.Count;
        }
        
        /// <summary>
        /// Tries to copy data from a source list to a destination compute buffer.
        /// If the destination is null or the wrong size, it is disposed and a new one is created.
        /// </summary>
        private static void WriteBufferData<T>(List<T> src, ref ComputeBuffer dst) where T : unmanaged {
            // Dispose and allocate new buffer if necessary.
            if (dst == null || dst.count != src.Count) {
                dst?.Dispose();
                var count = src.Count > 0 ? src.Count : 1;
                dst = new ComputeBuffer(count, Marshal.SizeOf<T>(), ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
            }
            
            // Write in new data or dummy data to prevent zero-sized buffers.
            var na = dst.BeginWrite<T>(0, src.Count > 0 ? src.Count : 1);
            if (src.Count > 0) {
                for (var i = 0; i < src.Count; i++) {
                    na[i] = src[i];
                }
            }
            else {
                na[0] = default;
            }
            dst.EndWrite<T>(src.Count > 0 ? src.Count : 1);
        }

        private void BuildCommandBuffer() {
            _commandBuffer.Clear();
            _commandBuffer.SetRenderTarget(_resultTexture);
            _commandBuffer.ClearRenderTarget(true, true, Color.black);
            
            // Update camera matrices.
            _commandBuffer.SetComputeMatrixParam(_rayMarch, _idInverseProjection, _camera.projectionMatrix.inverse);
            _commandBuffer.SetComputeMatrixParam(_rayMarch, _idInverseWorld, _camera.worldToCameraMatrix.inverse);
            _commandBuffer.SetComputeFloatParam(_rayMarch, _idPixelSize, _camera.projectionMatrix.m11 / _resultTexture.width);
            
            // Update frame even indicator (checkerboarding).
            _commandBuffer.SetComputeIntParam(_rayMarch, _idFrameEven, Time.frameCount & 1);

            // Configure initialization kernel.
            _commandBuffer.SetComputeIntParam(_rayMarch, _idPixelsPerBatch, _pixelsPerBatch);
            
            _commandBuffer.SetComputeBufferParam(_rayMarch, KERNEL_ID_INITIALIZE, _idPointLights, _pointLightBuffer);
            _commandBuffer.SetComputeBufferParam(_rayMarch, KERNEL_ID_INITIALIZE, _idSpotLights, _spotLightBuffer);
            _commandBuffer.SetComputeBufferParam(_rayMarch, KERNEL_ID_INITIALIZE, _idPolygonLights, _polygonLightBuffer);
            _commandBuffer.SetComputeBufferParam(_rayMarch, KERNEL_ID_INITIALIZE, _idLightSegments, _lightSegmentBuffer);
            
            _commandBuffer.SetComputeBufferParam(_rayMarch, KERNEL_ID_INITIALIZE, _idRectShapes, _rectBuffer);
            _commandBuffer.SetComputeBufferParam(_rayMarch, KERNEL_ID_INITIALIZE, _idCircleShapes, _circleBuffer);
            _commandBuffer.SetComputeBufferParam(_rayMarch, KERNEL_ID_INITIALIZE, _idPolygonShapes, _polygonBuffer);
            _commandBuffer.SetComputeBufferParam(_rayMarch, KERNEL_ID_INITIALIZE, _idShapeSegments, _shapeSegmentBuffer);
            
            _commandBuffer.SetComputeBufferParam(_rayMarch, KERNEL_ID_INITIALIZE, _idOutputRays, _rayBuffer);
            _commandBuffer.SetComputeBufferParam(_rayMarch, KERNEL_ID_INITIALIZE, _idRayCounter, _rayCounter);
            _commandBuffer.SetComputeBufferParam(_rayMarch, KERNEL_ID_INITIALIZE, _idAccumulation, _accumulationBuffer);

            // Configure march kernel.
            _commandBuffer.SetComputeBufferParam(_rayMarch, KERNEL_ID_MARCH, _idPointLights, _pointLightBuffer);
            _commandBuffer.SetComputeBufferParam(_rayMarch, KERNEL_ID_MARCH, _idSpotLights, _spotLightBuffer);
            _commandBuffer.SetComputeBufferParam(_rayMarch, KERNEL_ID_MARCH, _idPolygonLights, _polygonLightBuffer);
            _commandBuffer.SetComputeBufferParam(_rayMarch, KERNEL_ID_MARCH, _idLightSegments, _lightSegmentBuffer);
            
            _commandBuffer.SetComputeBufferParam(_rayMarch, KERNEL_ID_MARCH, _idRectShapes, _rectBuffer);
            _commandBuffer.SetComputeBufferParam(_rayMarch, KERNEL_ID_MARCH, _idCircleShapes, _circleBuffer);
            _commandBuffer.SetComputeBufferParam(_rayMarch, KERNEL_ID_MARCH, _idPolygonShapes, _polygonBuffer);
            _commandBuffer.SetComputeBufferParam(_rayMarch, KERNEL_ID_MARCH, _idShapeSegments, _shapeSegmentBuffer);
            
            _commandBuffer.SetComputeBufferParam(_rayMarch, KERNEL_ID_MARCH, _idInputRays, _rayBuffer);
            _commandBuffer.SetComputeBufferParam(_rayMarch, KERNEL_ID_MARCH, _idRayCounter, _rayCounter);

            _commandBuffer.SetComputeBufferParam(_rayMarch, KERNEL_ID_MARCH, _idAccumulation, _accumulationBuffer);
            
            // Configure finalization kernel.
            _commandBuffer.SetComputeBufferParam(_rayMarch, KERNEL_ID_FINALIZE, _idAccumulation, _accumulationBuffer);
            _commandBuffer.SetComputeTextureParam(_rayMarch, KERNEL_ID_FINALIZE, _idResult, _resultTexture);

            // Re-run the initialization and march kernels for each ray batch.
            for (var b = 0; b < _rayBatchCount; b++) {
                // Update batch offset.
                _commandBuffer.SetComputeIntParam(_rayMarch, _idBatchOffset, b * _pixelsPerBatch);

                // Clear ray buffer.
                _commandBuffer.SetBufferCounterValue(_rayBuffer, 0);

                // Dispatch initialization kernel.
                _commandBuffer.DispatchCompute(
                    _rayMarch, 
                    KERNEL_ID_INITIALIZE, 
                    Mathf.CeilToInt((float)_pixelsPerBatch / DISPATCH_GROUP_SIZE), 
                    1, 1
                );

                // Copy ray count to atomic counter buffer.
                _commandBuffer.CopyCounterValue(_rayBuffer, _rayCounter, 0);

                // Dispatch march kernel on current batch.
                _commandBuffer.DispatchCompute(
                    _rayMarch, 
                    KERNEL_ID_MARCH, 
                    Mathf.CeilToInt((float)_pixelsPerBatch * _lightCount / DISPATCH_GROUP_SIZE),
                    1, 1
                );
            }
            
            // Dispatch finalization kernel.
            _commandBuffer.DispatchCompute(
                _rayMarch, 
                KERNEL_ID_FINALIZE, 
                Mathf.CeilToInt((float)_resultTexture.width / 8), 
                Mathf.CeilToInt((float)_resultTexture.height / 8), 
                1
            );

            // Update global shader uniform.
            _commandBuffer.SetGlobalTexture(_idVektorLightMap, _resultTexture);
            
            // Reset render target.
            _commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
        }

        public void Render() {
            if (_rayCountMax == 0) {
                return;
            }

            // Build command buffer.
            BuildCommandBuffer();
            Graphics.ExecuteCommandBuffer(_commandBuffer);
        }

        ~RayMarchRenderer() {
            Dispose();
        }

        public void Dispose() {
            _pointLightBuffer?.Dispose();
            _spotLightBuffer?.Dispose();
            _polygonLightBuffer?.Dispose();
            _lightSegmentBuffer?.Dispose();
            _circleBuffer?.Dispose();
            _rectBuffer?.Dispose();
            _polygonBuffer?.Dispose();
            _shapeSegmentBuffer?.Dispose();
            _rayBuffer?.Dispose();
            _rayCounter?.Dispose();
            _accumulationBuffer?.Dispose();
            _commandBuffer?.Dispose();
            _resultTexture.Release();
        }
    }
}