using System;
using System.Runtime.InteropServices;
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
        private const int FrustumSize = 4 * 4 * sizeof(float);
        private const int LightIndexListBaseIndexOffset = 2;

        private readonly ComputeShaderKernel _computeFrustumsKernel;
        private readonly ComputeShaderKernel _cullLightsKernel;
        private readonly ToonStructuredComputeBuffer _frustumsBuffer = new(FrustumSize);
        private readonly ToonStructuredComputeBuffer _lightGrid = new(sizeof(uint) * 2);
        private readonly ToonStructuredComputeBuffer _lightIndexList = new(sizeof(uint));
        private readonly ToonLighting _lighting;
        private readonly ComputeShaderKernel _setupKernel;
        private readonly GlobalKeyword _tiledLightingKeyword;
        private readonly ToonStructuredComputeBuffer _tiledLightsBuffer =
            new(Marshal.SizeOf<TiledLight>(), ToonLighting.MaxAdditionalLightCountTiled / 8);

        private ScriptableRenderContext _context;
        private bool _enabled;

        private float _screenHeight;
        private float _screenWidth;
        private uint _tilesX;
        private uint _tilesY;

        public ToonTiledLighting(ToonLighting lighting)
        {
            _lighting = lighting;
            _tiledLightingKeyword = GlobalKeyword.Create("_TOON_RP_TILED_LIGHTING");

            ComputeShader clearCountersComputeShader = Resources.Load<ComputeShader>("TiledLighting_Setup");
            _setupKernel = new ComputeShaderKernel(clearCountersComputeShader, 0);

            ComputeShader computeFrustumsComputeShader = Resources.Load<ComputeShader>("TiledLighting_ComputeFrustums");
            _computeFrustumsKernel = new ComputeShaderKernel(computeFrustumsComputeShader, 0);

            ComputeShader cullLightsComputeShader = Resources.Load<ComputeShader>("TiledLighting_CullLights");
            _cullLightsKernel = new ComputeShaderKernel(cullLightsComputeShader, 0);
        }

        private int TotalTilesCount => (int) (_tilesX * _tilesY);

        public void Dispose()
        {
            _frustumsBuffer?.Dispose();
            _lightGrid?.Dispose();
            _lightIndexList?.Dispose();
            _tiledLightsBuffer?.Dispose();
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

            _frustumsBuffer.Update(totalTilesCount);
            _lightGrid.Update(totalTilesCount * 2);
            _lightIndexList.Update(totalTilesCount * ReservedLightsPerTile * 2 + LightIndexListBaseIndexOffset);

            _lighting.GetTiledAdditionalLightsBuffer(out _, out int tiledLightsCount);
            _tiledLightsBuffer.Update(tiledLightsCount);

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
                    _lighting.GetTiledAdditionalLightsBuffer(out TiledLight[] tiledLights, out int tiledLightsCount);
                    _tiledLightsBuffer.Buffer.SetData(tiledLights, 0, 0, tiledLightsCount);
                    cmd.SetGlobalBuffer("_TiledLighting_Lights", _tiledLightsBuffer.Buffer);

                    cmd.SetGlobalVector("_TiledLighting_ScreenDimensions",
                        new Vector4(_screenWidth, _screenHeight)
                    );
                    cmd.SetGlobalInt("_TiledLighting_TilesY", (int) _tilesY);
                    cmd.SetGlobalInt("_TiledLighting_TilesX", (int) _tilesX);
                    cmd.SetGlobalInt("_TiledLighting_CurrentLightIndexListOffset", 0);
                    cmd.SetGlobalInt("_TiledLighting_CurrentLightGridOffset", 0);

                    using (new ProfilingScope(cmd, NamedProfilingSampler.Get("Clear Counters")))
                    {
                        cmd.SetGlobalBuffer("_TiledLighting_LightIndexList", _lightIndexList.Buffer);
                        _setupKernel.Dispatch(cmd, 1);
                    }

                    using (new ProfilingScope(cmd, NamedProfilingSampler.Get("Compute Frustums")))
                    {
                        cmd.SetGlobalBuffer("_TiledLighting_Frustums", _frustumsBuffer.Buffer);
                        _computeFrustumsKernel.Dispatch(cmd, _tilesX, _tilesY);
                    }

                    using (new ProfilingScope(cmd, NamedProfilingSampler.Get("Cull Lights")))
                    {
                        // Frustum and light index list buffers are already bound
                        cmd.SetGlobalBuffer("_TiledLighting_LightGrid", _lightGrid.Buffer);
                        _cullLightsKernel.Dispatch(cmd, (uint) _screenWidth, (uint) _screenHeight);
                    }
                }
            }

            _context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void PrepareForOpaqueGeometry(CommandBuffer cmd)
        {
            PrepareForGeometryPass(cmd, 0);
        }

        public void PrepareForTransparentGeometry(CommandBuffer cmd)
        {
            PrepareForGeometryPass(cmd, TotalTilesCount);
        }

        private void PrepareForGeometryPass(CommandBuffer cmd, int offset)
        {
            cmd.SetGlobalInt("_TiledLighting_CurrentLightIndexListOffset",
                LightIndexListBaseIndexOffset + offset * ReservedLightsPerTile
            );
            cmd.SetGlobalInt("_TiledLighting_CurrentLightGridOffset", offset);
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