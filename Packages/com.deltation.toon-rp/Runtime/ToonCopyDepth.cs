using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public class ToonCopyDepth : IDisposable
    {
        public const string ShaderName = "Hidden/Toon RP/CopyDepth";
        private readonly ToonPipelineMaterial _copyDepthMaterial = new(ShaderName, "Toon RP Copy Depth");

        private Camera _camera;

        private ToonCameraRenderTarget _renderTarget;

        public void Dispose()
        {
            _copyDepthMaterial.Dispose();
        }

        public void Setup(Camera camera, ToonCameraRenderTarget renderTarget)
        {
            _camera = camera;
            _renderTarget = renderTarget;
        }

        public void Copy(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination)
        {
            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.CopyDepth)))
            {
                int msaaSamples = _renderTarget.EffectiveMsaaSamples;
                Material material = _copyDepthMaterial.GetOrCreate();
                Shader shader = _copyDepthMaterial.Shader;

                var msaa2Keyword = new LocalKeyword(shader, ShaderKeywords.DepthMsaa2);
                var msaa4Keyword = new LocalKeyword(shader, ShaderKeywords.DepthMsaa4);
                var msaa8Keyword = new LocalKeyword(shader, ShaderKeywords.DepthMsaa8);

                switch (msaaSamples)
                {
                    case 8:
                        cmd.DisableKeyword(material, msaa2Keyword);
                        cmd.DisableKeyword(material, msaa4Keyword);
                        cmd.EnableKeyword(material, msaa8Keyword);
                        break;

                    case 4:
                        cmd.DisableKeyword(material, msaa2Keyword);
                        cmd.EnableKeyword(material, msaa4Keyword);
                        cmd.DisableKeyword(material, msaa8Keyword);
                        break;

                    case 2:
                        cmd.EnableKeyword(material, msaa2Keyword);
                        cmd.DisableKeyword(material, msaa4Keyword);
                        cmd.DisableKeyword(material, msaa8Keyword);
                        break;

                    default:
                        cmd.DisableKeyword(material, msaa2Keyword);
                        cmd.DisableKeyword(material, msaa4Keyword);
                        cmd.DisableKeyword(material, msaa8Keyword);
                        break;
                }

                cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                cmd.SetViewport(_renderTarget.PixelRect);

                bool renderToTexture = _camera.targetTexture != null;
                if (_renderTarget.MsaaSamples > 1)
                {
                    cmd.SetGlobalTexture(ToonBlitter.MainTexId, (RenderTexture) null);
                    cmd.SetGlobalTexture(ToonBlitter.MainTexMsId, source);
                }
                else
                {
                    cmd.SetGlobalTexture(ToonBlitter.MainTexId, source);
                    cmd.SetGlobalTexture(ToonBlitter.MainTexMsId, (RenderTexture) null);
                }

                ToonBlitter.Blit(cmd, material, renderToTexture, 0);
            }
        }

        private static class ShaderKeywords
        {
            public const string DepthMsaa2 = "_DEPTH_MSAA_2";
            public const string DepthMsaa4 = "_DEPTH_MSAA_4";
            public const string DepthMsaa8 = "_DEPTH_MSAA_8";
        }
    }
}