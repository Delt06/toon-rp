using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using static DELTation.ToonRP.Extensions.BuiltIn.ToonOffScreenTransparencySettings;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    public class ToonOffScreenTransparency : ToonRenderingExtensionBase
    {
        public const string ShaderName = "Hidden/Toon RP/Off-Screen Transparency";
        private static readonly int ColorId = Shader.PropertyToID("_ToonRP_CompositeTransparency_Color");
        private static readonly int DepthId = Shader.PropertyToID("_ToonRP_CompositeTransparency_Depth");
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int PatternId = Shader.PropertyToID("_Pattern");
        private static readonly int PatternHorizontalTilingId = Shader.PropertyToID("_PatternHorizontalTiling");
        private static readonly int HeightOverWidthId = Shader.PropertyToID("_HeightOverWidth");
        private static readonly int BlendSrcId = Shader.PropertyToID("_BlendSrc");
        private static readonly int BlendDstId = Shader.PropertyToID("_BlendDst");
        private readonly ToonDepthDownsample _depthDownsample = new();

        private readonly DepthPrePass _depthPrePass = new(
            DepthId,
            0
        );
        private readonly Material _material =
            ToonRpUtils.CreateEngineMaterial(ShaderName, "Toon RP Off-Screen Transparency");
        private Camera _camera;
        private ToonCameraRendererSettings _cameraRendererSettings;
        private ToonCameraRenderTarget _cameraRenderTarget;
        private CullingResults _cullingResults;
        private int _height;
        private ToonOffScreenTransparencySettings _settings;
        private ScriptableRenderContext _srpContext;
        private int _width;

        public override void Setup(in ToonRenderingExtensionContext context,
            IToonRenderingExtensionSettingsStorage settingsStorage)
        {
            base.Setup(in context, settingsStorage);
            _srpContext = context.ScriptableRenderContext;
            _cullingResults = context.CullingResults;
            _settings = settingsStorage.GetSettings<ToonOffScreenTransparencySettings>(this);
            _camera = context.Camera;
            _cameraRendererSettings = context.CameraRendererSettings;
            _cameraRenderTarget = context.CameraRenderTarget;

            _width = Mathf.Max(1, _cameraRenderTarget.Width / _settings.ResolutionFactor);
            _height = Mathf.Max(1, _cameraRenderTarget.Height / _settings.ResolutionFactor);
        }

        public override void Render()
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            string passName = !string.IsNullOrWhiteSpace(_settings.PassName)
                ? _settings.PassName
                : "Off-Screen Transparency";
            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(passName)))
            {
                _srpContext.ExecuteCommandBufferAndClear(cmd);

                if (_settings.DepthMode == DepthRenderMode.PrePass)
                {
                    const bool stencil = true;
                    _depthPrePass.Setup(_srpContext, _cullingResults,
                        _camera, _cameraRendererSettings,
                        PrePassMode.Depth,
                        _width, _height,
                        stencil
                    );

                    _depthPrePass.Render();
                }

                using (new ProfilingScope(cmd, NamedProfilingSampler.Get("Render Transparent Geometry")))
                {
                    {
                        cmd.GetTemporaryRT(ColorId, _width, _height, 0, FilterMode.Bilinear, RenderTextureFormat.Default
                        );
                        if (_settings.DepthMode == DepthRenderMode.Downsample)
                        {
                            cmd.GetTemporaryRT(DepthId,
                                new RenderTextureDescriptor(_width, _height, GraphicsFormat.None,
                                    GraphicsFormat.D24_UNorm_S8_UInt
                                )
                            );
                        }

                        cmd.SetRenderTarget(ColorId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                            DepthId, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
                        );
                        cmd.ClearRenderTarget(false, true, Color.black);
                        if (_settings.DepthMode == DepthRenderMode.Downsample)
                        {
                            bool highQuality = _settings.DepthDownsampleQuality == DepthDownsampleQualityLevel.High;
                            _depthDownsample.Downsample(cmd, highQuality, _settings.ResolutionFactor);
                        }
                    }

                    _cameraRenderTarget.SetScreenParamsOverride(cmd, _width, _height);
                    _srpContext.ExecuteCommandBufferAndClear(cmd);

                    {
                        var sortingSettings = new SortingSettings(_camera)
                        {
                            criteria = SortingCriteria.CommonTransparent,
                        };
                        // See 23-3: https://developer.nvidia.com/gpugems/gpugems3/part-iv-image-effects/chapter-23-high-speed-screen-particles
                        RenderTargetBlendState renderTargetBlendState = _settings.BlendMode switch
                        {
                            TransparencyBlendMode.Alpha => new RenderTargetBlendState(
                                sourceColorBlendMode: BlendMode.SrcAlpha,
                                destinationColorBlendMode: BlendMode.OneMinusSrcAlpha,
                                sourceAlphaBlendMode: BlendMode.Zero,
                                destinationAlphaBlendMode: BlendMode.OneMinusSrcAlpha
                            ),
                            TransparencyBlendMode.Additive => new RenderTargetBlendState(
                                sourceColorBlendMode: BlendMode.One, destinationColorBlendMode: BlendMode.One
                            ),
                            _ => throw new ArgumentOutOfRangeException(),
                        };
                        ToonCameraRenderer.DrawGeometry(_cameraRendererSettings,
                            ref _srpContext, _cullingResults, sortingSettings, RenderQueueRange.transparent,
                            _settings.LayerMask,
                            new RenderStateBlock(RenderStateMask.Blend)
                            {
                                blendState = new BlendState
                                {
                                    blendState0 = renderTargetBlendState,
                                },
                            }
                        );
                    }
                }

                using (new ProfilingScope(cmd, NamedProfilingSampler.Get("Compose with Camera Render Target")))
                {
                    _cameraRenderTarget.SetRenderTarget(cmd);
                    _material.SetVector(TintId, _settings.Tint);
                    _material.SetTexture(PatternId,
                        _settings.Pattern != null ? _settings.Pattern : Texture2D.whiteTexture
                    );
                    _material.SetFloat(PatternHorizontalTilingId, _settings.PatternHorizontalTiling);
                    _material.SetFloat(HeightOverWidthId, (float) _height / _width);

                    (BlendMode blendSource, BlendMode blendDestination) = _settings.BlendMode switch
                    {
                        TransparencyBlendMode.Alpha => (BlendMode.OneMinusSrcAlpha, BlendMode.SrcAlpha),
                        TransparencyBlendMode.Additive => (BlendMode.One, BlendMode.One),
                        _ => throw new ArgumentOutOfRangeException(),
                    };
                    _material.SetFloat(BlendSrcId, (float) blendSource);
                    _material.SetFloat(BlendDstId, (float) blendDestination);

                    ToonBlitter.Blit(cmd, _material);
                }


                cmd.ReleaseTemporaryRT(ColorId);
                switch (_settings.DepthMode)
                {
                    case DepthRenderMode.Downsample:
                        cmd.ReleaseTemporaryRT(DepthId);
                        break;
                    case DepthRenderMode.PrePass:
                        _depthPrePass.Cleanup();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                _srpContext.ExecuteCommandBufferAndClear(cmd);
            }

            _srpContext.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}