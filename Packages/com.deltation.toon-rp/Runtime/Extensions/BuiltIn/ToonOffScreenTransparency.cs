using System;
using UnityEngine;
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

        private readonly DepthPrePass _depthPrePass = new(
            DepthId,
            0
        );
        private Camera _camera;
        private ToonCameraRendererSettings _cameraRendererSettings;
        private ToonCameraRenderTarget _cameraRenderTarget;
        private CullingResults _cullingResults;
        private int _height;
        private Material _material;
        private ToonOffScreenTransparencySettings _settings;
        private Shader _shader;
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

        private void EnsureMaterialIsCreated()
        {
            if (_material != null && _shader != null)
            {
                return;
            }

            _shader = Shader.Find(ShaderName);
            _material = new Material(_shader)
            {
                name = "Toon RP Off-Screen Transparency",
            };
        }

        public override void Render()
        {
            EnsureMaterialIsCreated();
            CommandBuffer cmd = CommandBufferPool.Get();

            string passName = !string.IsNullOrWhiteSpace(_settings.PassName)
                ? _settings.PassName
                : "Off-Screen Transparency";
            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(passName)))
            {
                ExecuteBuffer(cmd);

                {
                    const bool stencil = true;
                    _depthPrePass.Setup(_srpContext, _cullingResults,
                        _camera, _cameraRendererSettings,
                        DepthPrePassMode.Depth,
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
                        cmd.SetRenderTarget(ColorId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                            DepthId, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
                        );
                        cmd.ClearRenderTarget(false, true, Color.black);
                        ExecuteBuffer(cmd);
                    }

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

                    CustomBlitter.Blit(cmd, _material);
                }


                ExecuteBuffer(cmd);
                _depthPrePass.Cleanup();
            }

            ExecuteBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void ExecuteBuffer(CommandBuffer cmd)
        {
            _srpContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}