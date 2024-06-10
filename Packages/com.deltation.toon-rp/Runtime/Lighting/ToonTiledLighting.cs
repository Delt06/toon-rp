using System;
using DELTation.ToonRP.Extensions;
using DELTation.ToonRP.Xr;
using JetBrains.Annotations;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Lighting
{
    public class ToonTiledLighting : IDisposable
    {
        private const int TileSize = 16;
        public const int MinLightsPerTile = 8;
        public const int MaxLightsPerTile = 64;
        private const int TileBoundsStride = 2 * 4 * 3 * sizeof(float);
        private const int LightIndexListBaseIndexOffset = 1;

        public const string SetupComputeShaderName = "TiledLighting_Setup";
        public const string ComputeTileBoundsComputeShaderName = "TiledLighting_ComputeTileBounds";
        public const string CullLightsComputeShaderName = "TiledLighting_CullLights";
        public const string TiledLightingKeywordName = "_TOON_RP_TILED_LIGHTING";
        private readonly ToonComputeBuffer _lightGrid = new(ComputeBufferType.Structured, sizeof(uint) * 2);
        private readonly ToonComputeBuffer _lightIndexList = new(ComputeBufferType.Structured, sizeof(uint));
        private readonly ToonLighting _lighting;

        private readonly ToonComputeBuffer _tileBoundsBuffer = new(ComputeBufferType.Structured, TileBoundsStride);
        private readonly GlobalKeyword _tiledLightingKeyword;
        private readonly ToonComputeBuffer _tiledLightsBuffer =
            new(ComputeBufferType.Structured, UnsafeUtility.SizeOf<TiledLight>(),
                ToonLighting.MaxAdditionalLightCountTiled / 8
            );

        private bool _computeShadersAreValid;

        private ComputeShaderKernel _computeTileBoundsKernel;

        private ScriptableRenderContext _context;
        private ComputeShaderKernel _cullLightsKernel;
        private bool _enabled;
        private int _reservedLightsPerTile;

        private float _screenHeight;
        private float _screenWidth;
        private ComputeShaderKernel _setupKernel;
        private uint _tilesX;
        private uint _tilesY;

        public ToonTiledLighting(ToonLighting lighting)
        {
            _lighting = lighting;
            _tiledLightingKeyword = GlobalKeyword.Create(TiledLightingKeywordName);
        }

        public void Dispose()
        {
            _tileBoundsBuffer?.Dispose();
            _lightGrid?.Dispose();
            _lightIndexList?.Dispose();
            _tiledLightsBuffer?.Dispose();
        }

        private void EnsureComputeShadersAreValid()
        {
            if (!_computeShadersAreValid)
            {
                _computeShadersAreValid = true;

                ComputeShader clearCountersComputeShader = Resources.Load<ComputeShader>(SetupComputeShaderName);
                _setupKernel = new ComputeShaderKernel(clearCountersComputeShader, 0);

                ComputeShader computeFrustumsComputeShader =
                    Resources.Load<ComputeShader>(ComputeTileBoundsComputeShaderName);
                _computeTileBoundsKernel = new ComputeShaderKernel(computeFrustumsComputeShader, 0);

                ComputeShader cullLightsComputeShader = Resources.Load<ComputeShader>(CullLightsComputeShaderName);
                _cullLightsKernel = new ComputeShaderKernel(cullLightsComputeShader, 0);
            }
        }

        public void Setup(in ScriptableRenderContext context, in ToonRenderingExtensionContext toonContext)
        {
            _context = context;
            _enabled = toonContext.CameraRendererSettings.IsTiledLightingEnabledAndSupported();

            if (!_enabled)
            {
                return;
            }

            ToonXr.EmitErrorOnXr(toonContext.AdditionalCameraData.XrPass, "Tiled Lighting");

            EnsureComputeShadersAreValid();

            ToonCameraRenderTarget renderTarget = toonContext.CameraRenderTarget;
            _screenWidth = renderTarget.Width;
            _screenHeight = renderTarget.Height;
            _tilesX = (uint) Mathf.CeilToInt(_screenWidth / TileSize);
            _tilesY = (uint) Mathf.CeilToInt(_screenHeight / TileSize);
            int totalTilesCount = (int) (_tilesX * _tilesY);

            _tileBoundsBuffer.Update(totalTilesCount);
            _lightGrid.Update(totalTilesCount);

            _reservedLightsPerTile = Mathf.Clamp(
                toonContext.CameraRendererSettings.MaxLightsPerTile,
                MinLightsPerTile,
                MaxLightsPerTile
            );
            _lightIndexList.Update(totalTilesCount * _reservedLightsPerTile + LightIndexListBaseIndexOffset);

            int lightsCount = _lighting.GetAdditionalLightsCount();
            _tiledLightsBuffer.Update(lightsCount);

            _computeTileBoundsKernel.Setup();
        }

        public void CullLights()
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            SetTiledLightingKeyword(cmd, _enabled);

            if (_enabled)
            {
                using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.TiledLighting)))
                {
                    ref readonly ToonLighting.TiledAdditionalLightsData lightsData = ref _lighting.GetTiledAdditionalLightsBuffers();
                    int lightsCount = _lighting.GetAdditionalLightsCount();

                    _tiledLightsBuffer.Buffer.SetData(lightsData.TiledLights, 0, 0, lightsCount);
                    cmd.SetGlobalBuffer(ShaderIds.LightsStructuredBufferId, _tiledLightsBuffer.Buffer);

                    cmd.SetGlobalVectorArray(ShaderIds.LightsConstantBufferColorsId, lightsData.Colors);
                    cmd.SetGlobalVectorArray(ShaderIds.LightsConstantBufferPositionsAttenuationsId,
                        lightsData.PositionsAttenuations
                    );
                    cmd.SetGlobalVectorArray(ShaderIds.LightsConstantBufferSpotDirAttenuationsId,
                        lightsData.SpotDirsAttenuations
                    );

                    cmd.SetGlobalVector(ShaderIds.ScreenDimensionsId,
                        new Vector4(_screenWidth, _screenHeight)
                    );
                    cmd.SetGlobalInt(ShaderIds.TilesXId, (int) _tilesX);
                    cmd.SetGlobalInt(ShaderIds.TilesYId, (int) _tilesY);
                    cmd.SetGlobalInt(ShaderIds.ReservedLightsPerTileId, _reservedLightsPerTile);

                    using (new ProfilingScope(cmd, NamedProfilingSampler.Get("Clear Counters")))
                    {
                        cmd.SetGlobalBuffer(ShaderIds.LightIndexListId, _lightIndexList.Buffer);
                        _setupKernel.Dispatch(cmd, 1);
                    }

                    using (new ProfilingScope(cmd, NamedProfilingSampler.Get("Compute Tile Bounds")))
                    {
                        cmd.SetGlobalBuffer(ShaderIds.TileBoundsId, _tileBoundsBuffer.Buffer);
                        _computeTileBoundsKernel.Dispatch(cmd, _tilesX, _tilesY);
                    }

                    using (new ProfilingScope(cmd, NamedProfilingSampler.Get("Cull Lights")))
                    {
                        // Frustum and light index list buffers are already bound
                        cmd.SetGlobalBuffer(ShaderIds.LightGridId, _lightGrid.Buffer);
                        _cullLightsKernel.Dispatch(cmd, (uint) _screenWidth, (uint) _screenHeight);
                    }
                }
            }

            _context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void SetTiledLightingKeyword(CommandBuffer cmd, bool enabled)
        {
            cmd.SetKeyword(_tiledLightingKeyword, enabled);
            _lighting.SetAdditionalLightsKeywords(cmd, ToonCameraRendererSettings.AdditionalLightsMode.Off);
        }

        private static class ShaderIds
        {
            public static readonly int LightsStructuredBufferId = Shader.PropertyToID("_TiledLighting_Lights_SB");
            public static readonly int LightsConstantBufferColorsId = Shader.PropertyToID("_TiledLighting_Light_Color");
            public static readonly int LightsConstantBufferPositionsAttenuationsId =
                Shader.PropertyToID("_TiledLighting_Light_PositionsWs_Attenuation");
            public static readonly int LightsConstantBufferSpotDirAttenuationsId =
                Shader.PropertyToID("_TiledLighting_Light_SpotDir_Attenuation");
            public static readonly int ScreenDimensionsId = Shader.PropertyToID("_TiledLighting_ScreenDimensions");
            public static readonly int LightIndexListId = Shader.PropertyToID("_TiledLighting_LightIndexList");
            public static readonly int TileBoundsId = Shader.PropertyToID("_TiledLighting_TileBounds");
            public static readonly int LightGridId = Shader.PropertyToID("_TiledLighting_LightGrid");
            public static readonly int TilesYId = Shader.PropertyToID("_TiledLighting_TilesY");
            public static readonly int TilesXId = Shader.PropertyToID("_TiledLighting_TilesX");
            public static readonly int ReservedLightsPerTileId =
                Shader.PropertyToID("_TiledLighting_ReservedLightsPerTile");
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

            private ComputeShader Cs { get; }

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