using System;
using DELTation.ToonRP.Extensions;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Lighting
{
    public class ToonTiledLighting : IDisposable
    {
        private const int TileSize = 16;
        private readonly ComputeShaderWrapper _computeFrustumsShader;
        private readonly ComputeShader _shader;

        private ScriptableRenderContext _context;
        private ComputeBuffer _frustumsBuffer;
        private float _screenHeight;
        private float _screenWidth;
        private uint _tilesX;
        private uint _tilesY;

        public ToonTiledLighting()
        {
            _shader = Resources.Load<ComputeShader>("TiledLighting");

            _computeFrustumsShader = new ComputeShaderWrapper(
                _shader, 0
            );
        }

        public void Dispose()
        {
            _frustumsBuffer?.Dispose();
        }

        public void Setup(in ScriptableRenderContext context, in ToonRenderingExtensionContext toonContext)
        {
            _context = context;

            ToonCameraRenderTarget renderTarget = toonContext.CameraRenderTarget;
            _screenWidth = renderTarget.Width;
            _screenHeight = renderTarget.Height;
            _tilesX = (uint) Mathf.CeilToInt(_screenWidth / TileSize);
            _tilesY = (uint) Mathf.CeilToInt(_screenHeight / TileSize);
            int totalTilesCount = (int) (_tilesX * _tilesY);

            if (_frustumsBuffer == null || !_frustumsBuffer.IsValid() || _frustumsBuffer.count != totalTilesCount)
            {
                _frustumsBuffer?.Release();
                const int frustumSize = 4 * 4 * sizeof(float); // 4 planes, which are normal + distance
                _frustumsBuffer = new ComputeBuffer(totalTilesCount, frustumSize, ComputeBufferType.Structured);
            }

            _computeFrustumsShader.Setup();
        }

        public void CullLights()
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.TiledLighting)))
            {
                cmd.SetComputeVectorParam(_shader, "_ScreenDimensions", new Vector4(_screenWidth, _screenHeight));
                cmd.SetComputeIntParam(_shader, "_TilesX", (int) _tilesX);
                cmd.SetComputeIntParam(_shader, "_TilesY", (int) _tilesY);

                using (new ProfilingScope(cmd, NamedProfilingSampler.Get("Compute Frustums")))
                {
                    cmd.SetGlobalBuffer("_Frustums", _frustumsBuffer);
                    _computeFrustumsShader.Dispatch(cmd, _tilesX, _tilesY);
                }
            }

            _context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }

        private class ComputeShaderWrapper
        {
            private readonly ComputeShader _computeShader;
            private readonly int _kernelIndex;
            private uint _groupSizeX;
            private uint _groupSizeY;
            private uint _groupSizeZ;

            public ComputeShaderWrapper(ComputeShader computeShader, int kernelIndex)
            {
                _computeShader = computeShader;
                _kernelIndex = kernelIndex;
                Setup();
            }

            public void Setup()
            {
                _computeShader.GetKernelThreadGroupSizes(_kernelIndex,
                    out _groupSizeX, out _groupSizeY, out _groupSizeZ
                );
            }

            public void Dispatch(CommandBuffer cmd, uint totalThreadsX, uint totalThreadsY = 1, uint totalThreadsZ = 1)
            {
                cmd.DispatchCompute(_computeShader, _kernelIndex,
                    Mathf.CeilToInt((float) totalThreadsX / _groupSizeX),
                    Mathf.CeilToInt((float) totalThreadsY / _groupSizeY),
                    Mathf.CeilToInt((float) totalThreadsZ / _groupSizeZ)
                );
            }
        }
    }
}