﻿using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    public class ToonRenderObjects : ToonRenderingExtensionBase
    {
        private ToonAdditionalCameraData _additionalCameraData;
        private Camera _camera;
        private ToonCameraRendererSettings _cameraRendererSettings;
        private ToonCameraRenderTarget _cameraRenderTarget;
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;
        private ShaderTagId[] _lightModeTags = new ShaderTagId[1];
        private ToonRenderObjectsSettings _settings;

        public override void Setup(in ToonRenderingExtensionContext context,
            IToonRenderingExtensionSettingsStorage settingsStorage)
        {
            base.Setup(in context, settingsStorage);
            _settings = settingsStorage.GetSettings<ToonRenderObjectsSettings>(this);
            _context = context.ScriptableRenderContext;
            _camera = context.Camera;
            _cameraRendererSettings = context.CameraRendererSettings;
            _cullingResults = context.CullingResults;
            _cameraRenderTarget = context.CameraRenderTarget;
            _additionalCameraData = context.AdditionalCameraData;
        }

        public override void Render()
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            string passName = !string.IsNullOrWhiteSpace(_settings.PassName)
                ? _settings.PassName
                : ToonRpPassId.RenderObjects;

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(passName)))
            {
                bool overrideLightModeTags = false;
                string[] lightModeTags = _settings.Filters.LightModeTags;
                if (lightModeTags is { Length: > 0 })
                {
                    overrideLightModeTags = true;

                    Array.Resize(ref _lightModeTags, lightModeTags.Length);

                    for (int index = 0; index < lightModeTags.Length; index++)
                    {
                        _lightModeTags[index] = new ShaderTagId(lightModeTags[index]);
                    }
                }

                var cameraOverride = new ToonCameraOverride(_camera, _additionalCameraData, _cameraRenderTarget);
                cameraOverride.OverrideIfEnabled(cmd, _settings.Overrides.Camera);
                _context.ExecuteCommandBufferAndClear(cmd);

                bool opaque = _settings.Filters.Queue == ToonRenderObjectsSettings.FilterSettings.RenderQueue.Opaque;
                SortingCriteria sortingCriteria = opaque
                    ? SortingCriteria.CommonOpaque
                    : SortingCriteria.CommonTransparent;

                ClearRenderTargetIfEnabled(cmd);

                RenderStateBlock? renderStateBlock = ConstructRenderStateBlock();
                RenderQueueRange renderQueueRange = opaque ? RenderQueueRange.opaque : RenderQueueRange.transparent;
                bool includesTransparent = !opaque;
                ToonCameraRenderer.DrawGeometry(_cameraRendererSettings, ref _context, cmd, _camera, _cullingResults,
                    sortingCriteria,
                    renderQueueRange, includesTransparent, _settings.Filters.LayerMask, renderStateBlock,
                    overrideLightModeTags ? _lightModeTags : null,
                    false,
                    _settings.Overrides.Material
                );

                cameraOverride.Restore(cmd);
            }

            _context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void ClearRenderTargetIfEnabled(CommandBuffer cmd)
        {
            RTClearFlags clearFlags = RTClearFlags.None;

            if (_settings.ClearDepth)
            {
                clearFlags |= RTClearFlags.Depth;
            }

            if (_settings.ClearStencil)
            {
                clearFlags |= RTClearFlags.Stencil;
            }

            if (clearFlags != RTClearFlags.None)
            {
                cmd.ClearRenderTarget(clearFlags, Color.clear, 1.0f, 0);
                _context.ExecuteCommandBufferAndClear(cmd);
            }
        }

        private RenderStateBlock? ConstructRenderStateBlock()
        {
            ref readonly ToonRenderObjectsSettings.OverrideSettings overrides = ref _settings.Overrides;
            if (!overrides.Depth.Enabled && !overrides.Stencil.Enabled)
            {
                return null;
            }

            var renderStateBlock = new RenderStateBlock();
            if (overrides.Depth.Enabled)
            {
                renderStateBlock.mask |= RenderStateMask.Depth;
                renderStateBlock.depthState = new DepthState(overrides.Depth.WriteDepth, overrides.Depth.DepthTest);
            }

            if (overrides.Stencil.Enabled)
            {
                renderStateBlock.mask |= RenderStateMask.Stencil;
                renderStateBlock.stencilReference = overrides.Stencil.Value;
                renderStateBlock.stencilState = new StencilState(true,
                    overrides.Stencil.ReadMask, overrides.Stencil.WriteMask,
                    overrides.Stencil.CompareFunction,
                    overrides.Stencil.Pass,
                    overrides.Stencil.Fail,
                    overrides.Stencil.ZFail
                );
            }

            return renderStateBlock;
        }
    }
}