using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public sealed class ToonCameraRenderTarget : IDisposable
    {
        public const string FinalBlitShaderName = "Hidden/Toon RP/FinalBlit";

        private static readonly int ToonScreenParamsId = Shader.PropertyToID("_ToonRP_ScreenParams");
        private static readonly int ScreenParamsId = Shader.PropertyToID("_ScreenParams");
        private static readonly int BlitSourceId = Shader.PropertyToID("_BlitSource");

#if ENABLE_VR && ENABLE_XR_MODULE
        private readonly ToonCopyDepth _copyDepth = new();
#endif // ENABLE_VR && ENABLE_XR_MODULE

        private readonly ToonPipelineMaterial _finalBlitMaterial = new(FinalBlitShaderName, "Toon RP Final Blit");
        private ToonAdditionalCameraData _additionalCameraData;

        private Camera _camera;
        private FilterMode _filterMode;
        private bool _inRenderPass;

        private State _state;
        private bool _useNativeRenderPasses;
        public RenderTargetIdentifier CameraTargetColorId { get; private set; } = BuiltinRenderTextureType.CameraTarget;
        public RenderTargetIdentifier CameraTargetDepthId { get; private set; } = BuiltinRenderTextureType.CameraTarget;

        public bool ForceStoreAttachments { get; set; } = true;

        public int MsaaSamples { get; private set; }
        public int EffectiveMsaaSamples { get; private set; }
        public bool RenderToTexture { get; private set; }

        public GraphicsFormat ColorFormat { get; private set; }
        public GraphicsFormat DepthStencilFormat { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }
        public Rect PixelRect { get; private set; }

        public void Dispose()
        {
            _finalBlitMaterial?.Dispose();
#if ENABLE_VR && ENABLE_XR_MODULE
            _copyDepth.Dispose();
#endif // ENABLE_VR && ENABLE_XR_MODULE
        }

        public void ConfigureNativeRenderPasses(bool useNativeRenderPasses)
        {
            _useNativeRenderPasses = useNativeRenderPasses && ToonGraphicsApi.SupportsNativeRenderPasses;
        }

        public RenderTargetIdentifier CurrentColorBufferId(bool shouldBeAllocated = true)
        {
            if (shouldBeAllocated)
            {
                Assert.IsTrue(_state.ColorBufferId.IsAllocated);
            }

            return _state.ColorBufferId.Identifier;
        }

        public RenderTargetIdentifier CurrentDepthBufferId(bool shouldBeAllocated = true)
        {
            if (shouldBeAllocated)
            {
                Assert.IsTrue(_state.DepthBufferId.IsAllocated);
            }

            return _state.DepthBufferId.Identifier;
        }

        public void FinalBlit(CommandBuffer cmd, in ToonRenderPipelineSharedContext sharedContext,
            RenderTargetIdentifier? sourceOverride = default)
        {
            if (RenderToTexture)
            {
                bool colorLoad = CameraTargetColorId == BuiltinRenderTextureType.CameraTarget &&
                                 sharedContext.NumberOfCamerasUsingBackbufferAsFinalTarget > 0;
                RenderBufferLoadAction colorLoadAction =
                    colorLoad ? RenderBufferLoadAction.Load : RenderBufferLoadAction.DontCare;
                cmd.SetRenderTarget(CameraTargetColorId, colorLoadAction, RenderBufferStoreAction.Store);

                var screenParams = new ScreenParams(_camera.pixelWidth, _camera.pixelHeight)
                {
                    CameraRect = _camera.rect,
                    SetViewportRect = false,
                };
                SetScreenParamsOverride(cmd, screenParams);
                cmd.SetViewport(PixelRect);

                Material material = _finalBlitMaterial.GetOrCreate();
                cmd.SetGlobalTexture(BlitSourceId, sourceOverride ?? _state.ColorBufferId.Identifier);
                bool renderToTexture = _camera.targetTexture != null;
                ToonBlitter.Blit(cmd, material, renderToTexture, 0);

#if ENABLE_VR && ENABLE_XR_MODULE
                {
                    XRPass xrPass = _additionalCameraData.XrPass;
                    if (xrPass.enabled && xrPass.copyDepth)
                    {
                        const bool setupViewport = true;
                        var copyContext = new ToonCopyDepth.CopyContext(_camera, this, renderToTexture, setupViewport);
                        _copyDepth.Copy(cmd, copyContext, _state.DepthBufferId.Identifier, CameraTargetDepthId);
                    }
                }
#endif // ENABLE_VR && ENABLE_XR_MODULE
            }
        }


        public void Initialize(Camera camera, bool renderToTexture, int width, int height, Rect pixelRect,
            FilterMode filterMode,
            GraphicsFormat colorFormat, GraphicsFormat depthStencilFormat, int msaaSamples)
        {
            _filterMode = filterMode;
            RenderToTexture = renderToTexture;
            _camera = camera;
            Width = width;
            Height = height;
            PixelRect = pixelRect;
            ColorFormat = colorFormat;
            MsaaSamples = msaaSamples;
            EffectiveMsaaSamples = msaaSamples;
            DepthStencilFormat = depthStencilFormat;
            _state = default;
        }

        public void SetCameraTarget(RenderTargetIdentifier colorId, RenderTargetIdentifier depthId)
        {
            CameraTargetColorId = colorId;
            CameraTargetDepthId = depthId;
        }

        public void InitState(ToonAdditionalCameraData additionalCameraData)
        {
            _additionalCameraData = additionalCameraData;
            RTHandleSystem rtHandleSystem = additionalCameraData.RTHandleSystem;

            if (RenderToTexture)
            {
                MSAASamples msaaSamples = MSAASamples.None;

                if (!_useNativeRenderPasses && MsaaSamples > 1)
                {
                    msaaSamples = (MSAASamples) MsaaSamples;
                }

                EffectiveMsaaSamples = (int) msaaSamples;

                int arraySize = 1;

#if ENABLE_VR && ENABLE_XR_MODULE
                {
                    XRPass xrPass = additionalCameraData.XrPass;
                    if (xrPass.enabled)
                    {
                        arraySize = xrPass.viewCount;
                    }
                }
#endif // ENABLE_VR && ENABLE_XR_MODULE

                var dimensions = new Vector2Int(Width, Height);
                int depthBits = GraphicsFormatUtility.GetDepthBits(DepthStencilFormat);
                bool bindDepthTextureMs = msaaSamples != MSAASamples.None;
                const TextureWrapMode wrapMode = TextureWrapMode.Clamp;
                const FilterMode depthFilterMode = FilterMode.Point;

                rtHandleSystem.ReAllocateIfNeeded(ref additionalCameraData.IntermediateColorRt,
                    "_ToonRP_CameraColorBuffer", dimensions, arraySize, colorFormat: ColorFormat,
                    msaaSamples: msaaSamples, filterMode: _filterMode, wrapMode: wrapMode
                );
                _state.ColorBufferId = CameraRtId.Persistent(additionalCameraData.IntermediateColorRt);


                if (ForceStoreAttachments || !_useNativeRenderPasses)
                {
                    rtHandleSystem.ReAllocateIfNeeded(ref additionalCameraData.IntermediateDepthRt,
                        "_ToonRP_CameraDepthBuffer", dimensions, arraySize,
                        colorFormat: GraphicsFormat.None, depthBufferBits: depthBits,
                        msaaSamples: msaaSamples, bindTextureMs: bindDepthTextureMs,
                        filterMode: depthFilterMode, wrapMode: wrapMode
                    );
                    _state.DepthBufferId = CameraRtId.Persistent(additionalCameraData.IntermediateDepthRt);
                }
                else
                {
                    rtHandleSystem.ReleaseIfAllocated(ref additionalCameraData.IntermediateDepthRt);
                }
            }
            else
            {
                _state.ColorBufferId = CameraRtId.Persistent(CameraTargetColorId);
                _state.DepthBufferId = CameraRtId.Persistent(CameraTargetDepthId);

                rtHandleSystem.ReleaseIfAllocated(ref additionalCameraData.IntermediateColorRt);
                rtHandleSystem.ReleaseIfAllocated(ref additionalCameraData.IntermediateDepthRt);
            }
        }

        public void BeginRenderPass(ref ScriptableRenderContext context,
            RenderBufferLoadAction loadAction,
            in ToonClearValue clearValue = default)
        {
            Assert.IsFalse(_inRenderPass);

            CommandBuffer cmd = CommandBufferPool.Get();

            BeginRenderPass(ref context, cmd, loadAction, clearValue);

            SetScreenParams(cmd);
            context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
            _inRenderPass = true;
        }

        private void BeginRenderPass(ref ScriptableRenderContext context, CommandBuffer cmd,
            RenderBufferLoadAction loadAction,
            ToonClearValue clearValue)
        {
            int msaaSamples = _camera.allowMSAA ? MsaaSamples : 1;
            if (msaaSamples > 1 && (loadAction == RenderBufferLoadAction.Load ||
                                    ForceStoreAttachments && !ToonGraphicsApi.SupportsMultisampleDepthResolve()))
            {
                msaaSamples = 1;
            }

            if (_state.ColorBufferId.IsValid)
            {
                _state.ColorBufferId.EnsureTemporaryRT(cmd);
            }

            if (_state.DepthBufferId.IsValid)
            {
                _state.DepthBufferId.EnsureTemporaryRT(cmd);
            }

            context.ExecuteCommandBufferAndClear(cmd);

            if (_useNativeRenderPasses)
            {
                BeginNativeRenderPass(ref context, cmd, loadAction, clearValue, msaaSamples);
            }
            else
            {
                BeginFallbackRenderPass(ref context, cmd, loadAction, clearValue);
            }
        }

        private void BeginNativeRenderPass(ref ScriptableRenderContext context, CommandBuffer cmd,
            RenderBufferLoadAction loadAction,
            ToonClearValue clearValue, int msaaSamples)
        {
            const int colorIndex = 0;
            const int depthIndex = 1;

            bool usingMsaa = msaaSamples > 1;

            {
                var attachments =
                    new NativeArray<AttachmentDescriptor>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory
                    );

                var colorAttachment = new AttachmentDescriptor(ColorFormat);
                RenderBufferStoreAction storeAction =
                    usingMsaa ? RenderBufferStoreAction.Resolve : RenderBufferStoreAction.Store;
                {
                    colorAttachment.loadAction = loadAction;
                    colorAttachment.storeAction = storeAction;

                    if (clearValue.ClearColor)
                    {
                        colorAttachment.ConfigureClear(clearValue.BackgroundColor);
                    }

                    RenderTargetIdentifier colorAttachmentIdentifier =
                        ToonRpUtils.FixupTextureArrayIdentifier(_state.ColorBufferId.Identifier);

                    if (colorAttachment.loadAction == RenderBufferLoadAction.Load ||
                        colorAttachment.storeAction == RenderBufferStoreAction.Store)
                    {
                        colorAttachment.loadStoreTarget = colorAttachmentIdentifier;
                    }

                    if (colorAttachment.storeAction == RenderBufferStoreAction.Resolve)
                    {
                        colorAttachment.resolveTarget = colorAttachmentIdentifier;
                    }
                }

                var depthAttachment = new AttachmentDescriptor(DepthStencilFormat);
                {
                    depthAttachment.loadAction = loadAction;
                    depthAttachment.storeAction =
                        ForceStoreAttachments ? storeAction : RenderBufferStoreAction.DontCare;

                    if (clearValue.ClearDepth)
                    {
                        depthAttachment.ConfigureClear(Color.black);
                    }

                    RenderTargetIdentifier depthAttachmentIdentifier =
                        ToonRpUtils.FixupTextureArrayIdentifier(_state.DepthBufferId.Identifier);

                    if (depthAttachment.loadAction == RenderBufferLoadAction.Load ||
                        depthAttachment.storeAction == RenderBufferStoreAction.Store)
                    {
                        depthAttachment.loadStoreTarget = depthAttachmentIdentifier;
                    }

                    if (depthAttachment.storeAction == RenderBufferStoreAction.Resolve)
                    {
                        depthAttachment.resolveTarget = depthAttachmentIdentifier;
                    }

                    // Even if we don't store/resolve depth, we have to set anyway to prevent flickering on orientation change
                    if (!usingMsaa &&
                        depthAttachment.storeAction == RenderBufferStoreAction.DontCare &&
                        _state.DepthBufferId.Identifier == BuiltinRenderTextureType.CameraTarget
                       )
                    {
                        depthAttachment.loadStoreTarget = depthAttachmentIdentifier;
                    }

                    Assert.IsTrue(depthAttachment.resolveTarget == BuiltinRenderTextureType.None ||
                                  ToonGraphicsApi.SupportsMultisampleDepthResolve()
                    );

                    // Specifying camera depth more precisely is required here.
                    // For correct comparison, when have to fixup everything here.
                    RenderTargetIdentifier cameraTargetAllSlices =
                        ToonRpUtils.FixupTextureArrayIdentifier(BuiltinRenderTextureType.CameraTarget);
                    RenderTargetIdentifier cameraDepthAllSlices =
                        ToonRpUtils.FixupTextureArrayIdentifier(BuiltinRenderTextureType.Depth);

                    if (depthAttachment.loadStoreTarget == cameraTargetAllSlices)
                    {
                        depthAttachment.loadStoreTarget = cameraDepthAllSlices;
                    }

                    if (depthAttachment.resolveTarget == cameraTargetAllSlices)
                    {
                        depthAttachment.resolveTarget = cameraDepthAllSlices;
                    }
                }

                attachments[colorIndex] = colorAttachment;
                attachments[depthIndex] = depthAttachment;

                int fullScreenWidth = Width, fullScreenHeight = Height;

                if (!RenderToTexture)
                {
                    Rect cameraRect = _camera.rect;
                    if (cameraRect != new Rect(0, 0, 1, 1))
                    {
                        Rect pixelRect = _camera.pixelRect;
                        fullScreenWidth = Mathf.RoundToInt(pixelRect.width / cameraRect.width);
                        fullScreenHeight = Mathf.RoundToInt(pixelRect.height / cameraRect.height);
                    }
                }

                context.BeginRenderPass(fullScreenWidth, fullScreenHeight, msaaSamples, attachments, depthIndex);
                attachments.Dispose();
            }

            {
                var colorIndices = new NativeArray<int>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                colorIndices[0] = colorIndex;
                context.BeginSubPass(colorIndices);
                colorIndices.Dispose();
            }

            SetViewport(cmd);
            context.ExecuteCommandBufferAndClear(cmd);
        }

        private void BeginFallbackRenderPass(ref ScriptableRenderContext context, CommandBuffer cmd,
            RenderBufferLoadAction loadAction,
            ToonClearValue clearValue)
        {
            _state.ColorBufferId.EnsureTemporaryRT(cmd);
            _state.DepthBufferId.EnsureTemporaryRT(cmd);

            RenderTargetIdentifier colorId = _state.ColorBufferId.Identifier;
            RenderTargetIdentifier depthId = _state.DepthBufferId.Identifier;

            if (loadAction == RenderBufferLoadAction.Clear)
            {
                loadAction = RenderBufferLoadAction.DontCare;
            }

            const RenderBufferStoreAction colorStoreAction = RenderBufferStoreAction.Store;
            RenderBufferStoreAction depthStoreAction =
                ForceStoreAttachments ? RenderBufferStoreAction.Store : RenderBufferStoreAction.DontCare;

            cmd.SetRenderTarget(
                colorId, loadAction, colorStoreAction,
                depthId, loadAction, depthStoreAction
            );

            SetViewport(cmd);

            if (clearValue.ClearColor || clearValue.ClearDepth)
            {
                cmd.ClearRenderTarget(clearValue.ClearDepth, clearValue.ClearColor, clearValue.BackgroundColor);
            }

            context.ExecuteCommandBufferAndClear(cmd);
        }

        private void SetViewport(CommandBuffer cmd)
        {
            cmd.SetViewport(RenderToTexture ? new Rect(0, 0, Width, Height) : PixelRect);
        }

        public void EndRenderPass(ref ScriptableRenderContext context, CommandBuffer cmd)
        {
            Assert.IsTrue(_inRenderPass);

            if (_useNativeRenderPasses)
            {
                context.EndSubPass();
                context.EndRenderPass();
            }

            _inRenderPass = false;
        }

        public void ReleaseTemporaryRTs(CommandBuffer cmd)
        {
            _state.ColorBufferId.ReleaseTemporaryRT(cmd);
            _state.DepthBufferId.ReleaseTemporaryRT(cmd);
        }

        private void SetScreenParams(CommandBuffer cmd)
        {
            var screenParams = new ScreenParams(Width, Height);

            if (!RenderToTexture)
            {
                screenParams.CameraRect = _camera.rect;
            }

            SetScreenParamsOverride(cmd, screenParams);
        }

        public void SetScreenParamsOverride(CommandBuffer cmd, in ScreenParams screenParams)
        {
            float width = screenParams.Width;
            float height = screenParams.Height;

            cmd.SetGlobalVector(ToonScreenParamsId, new Vector4(
                    1.0f / width,
                    1.0f / height,
                    width,
                    height
                )
            );
            cmd.SetGlobalVector(ScreenParamsId, new Vector4(
                    width,
                    height,
                    1.0f + 1.0f / width,
                    1.0f + 1.0f / height
                )
            );

            ref readonly Rect cameraRect = ref screenParams.CameraRect;

            if (screenParams.SetViewportRect)
            {
                var viewportRect = new Vector4(
                    1.0f / cameraRect.width, 1.0f / cameraRect.height,
                    -cameraRect.xMin / cameraRect.width, -cameraRect.yMin / cameraRect.height
                );
                cmd.SetGlobalVector("_ToonRP_ViewportRect", viewportRect);
            }

            float fullscreenWidth = width / cameraRect.width;
            float fullscreenHeight = height / cameraRect.height;
            cmd.SetGlobalVector("_ToonRP_FullScreenParams", new Vector4(
                    1.0f / fullscreenWidth,
                    1.0f / fullscreenHeight,
                    fullscreenWidth,
                    fullscreenHeight
                )
            );
        }

        private struct State
        {
            public CameraRtId ColorBufferId;
            public CameraRtId DepthBufferId;
        }

        private struct CameraRtId
        {
            private readonly int _id;
            public readonly RenderTargetIdentifier Identifier;
            public readonly int ArraySize;
            private readonly RenderTextureDescriptor _descriptor;
            private readonly FilterMode _filterMode;
            private RtState _state;

            private CameraRtId(int id, RenderTextureDescriptor descriptor, FilterMode filterMode, int arraySize)
            {
                _id = id;
                Identifier = ToonRpUtils.FixupTextureArrayIdentifier(id);
                _descriptor = descriptor;
                _filterMode = filterMode;
                ArraySize = arraySize;
                _state = RtState.TempNotAllocated;
            }

            private CameraRtId(RenderTargetIdentifier identifier)
            {
                _id = 0;
                Identifier = ToonRpUtils.FixupTextureArrayIdentifier(identifier);
                _descriptor = default;
                _filterMode = FilterMode.Point;
                ArraySize = 1;
                _state = RtState.Persistent;
            }

            private CameraRtId(RTHandle rtHandle)
            {
                _id = 0;
                Identifier = ToonRpUtils.FixupTextureArrayIdentifier(rtHandle);
                _descriptor = rtHandle.rt.descriptor;
                _filterMode = rtHandle.rt.filterMode;
                ArraySize = rtHandle.rt.volumeDepth;
                _state = RtState.Persistent;
            }

            public static CameraRtId Temporary(int id, RenderTextureDescriptor descriptor, FilterMode filterMode,
                int arraySize = 1) =>
                new(id, descriptor, filterMode, arraySize);

            public static CameraRtId Persistent(RenderTargetIdentifier identifier) => new(identifier);
            public static CameraRtId Persistent(RTHandle rtHandle) => new(rtHandle);

            private enum RtState
            {
                Invalid,
                Persistent,
                TempNotAllocated,
                TempAllocated,
            }

            public void EnsureTemporaryRT(CommandBuffer cmd)
            {
                Assert.IsTrue(IsValid);

                if (_state != RtState.TempNotAllocated)
                {
                    return;
                }

                Assert.IsTrue(_id != 0);

                if (ArraySize > 1)
                {
                    cmd.GetTemporaryRTArray(_id, _descriptor.width, _descriptor.height, ArraySize,
                        _descriptor.depthBufferBits, _filterMode, _descriptor.graphicsFormat, _descriptor.msaaSamples
                    );
                }
                else
                {
                    cmd.GetTemporaryRT(_id, _descriptor, _filterMode);
                }

                _state = RtState.TempAllocated;
            }

            public void ReleaseTemporaryRT(CommandBuffer cmd)
            {
                if (_state == RtState.TempAllocated)
                {
                    Assert.IsTrue(_id != 0);
                    cmd.ReleaseTemporaryRT(_id);
                    _state = RtState.TempNotAllocated;
                }
            }

            public bool IsValid => _state != RtState.Invalid;
            public bool IsAllocated => _state is RtState.Persistent or RtState.TempAllocated;
        }

        public struct ScreenParams
        {
            public readonly int Width;
            public readonly int Height;
            public Rect CameraRect;
            public bool SetViewportRect;

            public ScreenParams(int width, int height)
            {
                Width = width;
                Height = height;
                CameraRect = new Rect(0, 0, 1, 1);
                SetViewportRect = true;
            }
        }
    }
}