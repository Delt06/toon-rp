using System;
using DELTation.ToonRP.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Lighting
{
    public class ToonTiledLighting : IDisposable
    {
        private const int TileSize = 16;
        private const int ReservedLightsPerTile = 2;

        private readonly ComputeShaderKernel _computeFrustumsKernel;
        private readonly ComputeShaderKernel _cullLightsKernel;
        private readonly ComputeBuffer _lightIndexCounter = new(2, sizeof(uint), ComputeBufferType.Structured);
        private readonly ComputeShaderKernel _setupKernel;
        private readonly GlobalKeyword _tiledLightingKeyword;

        private ScriptableRenderContext _context;
        private bool _enabled;
        private ComputeBuffer _frustumsBuffer;
        private ComputeBuffer _lightGrid;
        private ComputeBuffer _lightIndexList;
        private ComputeBuffer _lightIndexListTransparent;
        private float _screenHeight;
        private float _screenWidth;
        private uint _tilesX;
        private uint _tilesY;

        public ToonTiledLighting()
        {
            _tiledLightingKeyword = GlobalKeyword.Create("_TOON_RP_TILED_LIGHTING");

            ComputeShader clearCountersComputeShader = Resources.Load<ComputeShader>("TiledLighting_Setup");
            _setupKernel = new ComputeShaderKernel(clearCountersComputeShader, 0);

            ComputeShader computeFrustumsComputeShader = Resources.Load<ComputeShader>("TiledLighting_ComputeFrustums");
            _computeFrustumsKernel = new ComputeShaderKernel(computeFrustumsComputeShader, 0);

            ComputeShader cullLightsComputeShader = Resources.Load<ComputeShader>("TiledLighting_CullLights");
            _cullLightsKernel = new ComputeShaderKernel(cullLightsComputeShader, 0);
        }

        public void Dispose()
        {
            _frustumsBuffer?.Dispose();

            _lightIndexCounter?.Dispose();

            _lightGrid?.Dispose();

            _lightIndexList?.Dispose();
            _lightIndexListTransparent?.Dispose();
        }

        private static void ResizeBufferIfNeeded(ref ComputeBuffer buffer, int desiredCount, int stride)
        {
            if (buffer == null || !buffer.IsValid() || buffer.count < desiredCount)
            {
                buffer?.Release();
                buffer = new ComputeBuffer(desiredCount, stride, ComputeBufferType.Structured);
            }
        }

        public void Setup(in ScriptableRenderContext context, in ToonRenderingExtensionContext toonContext)
        {
            _context = context;
            _enabled = toonContext.CameraRendererSettings.IsTiledLightingEffectivelyEnabled;

            if (!_enabled)
            {
                return;
            }

            ToonCameraRenderTarget renderTarget = toonContext.CameraRenderTarget;
            _screenWidth = renderTarget.Width;
            _screenHeight = renderTarget.Height;
            _tilesX = (uint) Mathf.CeilToInt(_screenWidth / TileSize);
            _tilesY = (uint) Mathf.CeilToInt(_screenHeight / TileSize);
            int totalTilesCount = (int) (_tilesX * _tilesY);

            const int frustumSize = 4 * 4 * sizeof(float); // 4 planes, which are normal + distance
            ResizeBufferIfNeeded(ref _frustumsBuffer, totalTilesCount, frustumSize);
            ResizeBufferIfNeeded(ref _lightGrid, totalTilesCount * 2, sizeof(uint) * 2);
            ResizeBufferIfNeeded(ref _lightIndexList, totalTilesCount * ReservedLightsPerTile * 2, sizeof(uint));

            _computeFrustumsKernel.Setup();
        }

        public void CullLights()
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            cmd.SetKeyword(_tiledLightingKeyword, _enabled);

            if (_enabled)
            {
                using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.TiledLighting)))
                {
                    cmd.SetGlobalVector("_TiledLighting_ScreenDimensions",
                        new Vector4(_screenWidth, _screenHeight)
                    );
                    cmd.SetGlobalInt("_TiledLighting_TilesY", (int) _tilesY);
                    cmd.SetGlobalInt("_TiledLighting_TilesX", (int) _tilesX);
                    cmd.SetGlobalInt("_TiledLighting_CurrentLightIndexListOffset", 0);
                    cmd.SetGlobalInt("_TiledLighting_CurrentLightGridOffset", 0);

                    using (new ProfilingScope(cmd, NamedProfilingSampler.Get("Clear Counters")))
                    {
                        cmd.SetGlobalBuffer("_TiledLighting_LightIndexCounter", _lightIndexCounter);
                        _setupKernel.Dispatch(cmd, 1);
                    }

                    using (new ProfilingScope(cmd, NamedProfilingSampler.Get("Compute Frustums")))
                    {
                        cmd.SetGlobalBuffer("_TiledLighting_Frustums", _frustumsBuffer);
                        _computeFrustumsKernel.Dispatch(cmd, _tilesX, _tilesY);
                    }

                    using (new ProfilingScope(cmd, NamedProfilingSampler.Get("Cull Lights")))
                    {
                        // Frustums are already bound
                        cmd.SetGlobalBuffer("_TiledLighting_LightGrid", _lightGrid);
                        cmd.SetGlobalBuffer("_TiledLighting_LightIndexList", _lightIndexList);
                        _cullLightsKernel.Dispatch(cmd, (uint) _screenWidth, (uint) _screenHeight);
                    }
                }
            }

            _context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void PrepareForTransparents(CommandBuffer cmd)
        {
            int totalTiles = (int) (_tilesX * _tilesY);
            cmd.SetGlobalInt("_TiledLighting_CurrentLightIndexListOffset", totalTiles * ReservedLightsPerTile);
            cmd.SetGlobalInt("_TiledLighting_CurrentLightGridOffset", totalTiles);
        }

        private class ComputeShaderKernel
        {
            private readonly int _kernelIndex;
            private uint _groupSizeX;
            private uint _groupSizeY;
            private uint _groupSizeZ;

            public ComputeShaderKernel([NotNull] ComputeShader computeShader, int kernelIndex)
            {
                Cs = computeShader ? computeShader : throw new ArgumentNullException(nameof(computeShader));
                _kernelIndex = kernelIndex;
                Setup();
            }

            public ComputeShader Cs { get; }

            public void Setup()
            {
                Cs.GetKernelThreadGroupSizes(_kernelIndex,
                    out _groupSizeX, out _groupSizeY, out _groupSizeZ
                );
            }

            public void Dispatch(CommandBuffer cmd, uint totalThreadsX, uint totalThreadsY = 1, uint totalThreadsZ = 1)
            {
                cmd.DispatchCompute(Cs, _kernelIndex,
                    Mathf.CeilToInt((float) totalThreadsX / _groupSizeX),
                    Mathf.CeilToInt((float) totalThreadsY / _groupSizeY),
                    Mathf.CeilToInt((float) totalThreadsZ / _groupSizeZ)
                );
            }
        }
    }
}