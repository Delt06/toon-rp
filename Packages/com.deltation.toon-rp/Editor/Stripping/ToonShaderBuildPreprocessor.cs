﻿#if UNITY_EDITOR || !DEVELOPMENT_BUILD

using System;
using System.Collections.Generic;
using System.Linq;
using DELTation.ToonRP.Extensions;
using DELTation.ToonRP.Extensions.BuiltIn;
using DELTation.ToonRP.PostProcessing;
using DELTation.ToonRP.PostProcessing.BuiltIn;
using DELTation.ToonRP.Shadows;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Editor.Stripping
{
    [UsedImplicitly]
    public class ToonShaderBuildPreprocessor : IPreprocessShaders
    {
        private readonly List<ToonRenderPipelineAsset> _allToonRenderPipelineAssets;
        private readonly List<ShaderKeyword> _keywordsToStrip = new();
        private readonly List<(string shaderName, string keyword)> _localKeywordsToStrip = new();
        private readonly List<string> _shadersToStrip = new();

        public ToonShaderBuildPreprocessor()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);

            var renderPipelineAssets = new List<RenderPipelineAsset>();
            QualitySettings.GetAllRenderPipelineAssetsForPlatform(group.ToString(), ref renderPipelineAssets);
            renderPipelineAssets.Add(GraphicsSettings.currentRenderPipeline);

            _allToonRenderPipelineAssets = renderPipelineAssets
                .OfType<ToonRenderPipelineAsset>()
                .Distinct()
                .ToList();

            // Blob shadows
            if (_allToonRenderPipelineAssets.All(a => a.ShadowSettings.Mode != ToonShadowSettings.ShadowMode.Blobs))
            {
                _keywordsToStrip.Add(new ShaderKeyword(ToonShadows.BlobShadowsKeywordName));
                _shadersToStrip.Add(ToonBlobShadows.ShaderName);
            }

            // VSM
            if (_allToonRenderPipelineAssets.All(a => a.ShadowSettings.Mode != ToonShadowSettings.ShadowMode.Vsm))
            {
                _keywordsToStrip.Add(new ShaderKeyword(ToonShadows.VsmKeywordName));
                _keywordsToStrip.Add(new ShaderKeyword(ToonShadows.DirectionalShadowsKeywordName));
                _keywordsToStrip.Add(new ShaderKeyword(ToonShadows.DirectionalCascadedShadowsKeywordName));
            }

            // ToonRPVsmBlur
            {
                if (!_allToonRenderPipelineAssets.Any(a => a.ShadowSettings.Mode == ToonShadowSettings.ShadowMode.Vsm &&
                                                           a.ShadowSettings.Vsm.Blur !=
                                                           ToonVsmShadowSettings.BlurMode.None
                    ))
                {
                    _shadersToStrip.Add(ToonVsmShadows.BlurShaderName);
                }

                if (!_allToonRenderPipelineAssets.Any(a => a.ShadowSettings.Mode == ToonShadowSettings.ShadowMode.Vsm &&
                                                           a.ShadowSettings.Vsm.Blur ==
                                                           ToonVsmShadowSettings.BlurMode.HighQuality
                    ))
                {
                    _localKeywordsToStrip.Add((ToonVsmShadows.BlurShaderName, ToonVsmShadows.BlurHighQualityKeywordName)
                    );
                }

                if (!_allToonRenderPipelineAssets.Any(a => a.ShadowSettings.Mode == ToonShadowSettings.ShadowMode.Vsm &&
                                                           a.ShadowSettings.Vsm.IsBlurEarlyBailEnabled
                    ))
                {
                    _localKeywordsToStrip.Add((ToonVsmShadows.BlurShaderName, ToonVsmShadows.BlurEarlyBailKeywordName));
                }
            }

            // VSM without cascades
            if (!_allToonRenderPipelineAssets.Any(a => a.ShadowSettings.Mode == ToonShadowSettings.ShadowMode.Vsm &&
                                                       a.ShadowSettings.Vsm.Directional.Enabled &&
                                                       a.ShadowSettings.Vsm.Directional.CascadeCount == 1
                ))
            {
                _keywordsToStrip.Add(new ShaderKeyword(ToonShadows.DirectionalShadowsKeywordName));
            }

            // VSM with cascades
            if (!_allToonRenderPipelineAssets.Any(a => a.ShadowSettings.Mode == ToonShadowSettings.ShadowMode.Vsm &&
                                                       a.ShadowSettings.Vsm.Directional.Enabled &&
                                                       a.ShadowSettings.Vsm.Directional.CascadeCount > 1
                ))
            {
                _keywordsToStrip.Add(new ShaderKeyword(ToonShadows.DirectionalCascadedShadowsKeywordName));
            }

            // Crisp Anti-Aliased Ramp
            if (_allToonRenderPipelineAssets.All(a => a.GlobalRampSettings.Mode != ToonGlobalRampMode.CrispAntiAliased))
            {
                _keywordsToStrip.Add(new ShaderKeyword(ToonGlobalRamp.GlobalRampCrispKeywordName));
            }

            // Texture Ramp
            if (_allToonRenderPipelineAssets.All(a => a.GlobalRampSettings.Mode != ToonGlobalRampMode.Texture))
            {
                _keywordsToStrip.Add(new ShaderKeyword(ToonGlobalRamp.GlobalRampTextureKeywordName));
            }

            // Shadows Crisp Anti-Aliased Ramp
            if (!_allToonRenderPipelineAssets.Any(a => a.ShadowSettings.Mode != ToonShadowSettings.ShadowMode.Off &&
                                                       a.ShadowSettings.CrispAntiAliased
                ))
            {
                _keywordsToStrip.Add(new ShaderKeyword(ToonShadows.ShadowsRampCrispKeywordName));
            }

            // Shadows Pattern
            if (!_allToonRenderPipelineAssets.Any(a => a.ShadowSettings.Mode != ToonShadowSettings.ShadowMode.Off &&
                                                       a.ShadowSettings.Pattern != null
                ))
            {
                _keywordsToStrip.Add(new ShaderKeyword(ToonShadows.ShadowsPatternKeywordName));
            }

            // SSAO for forward shaders
            {
                if (!AnyExtension<ToonSsaoAsset>(ssao => ssao.Settings.Pattern == null))
                {
                    _keywordsToStrip.Add(new ShaderKeyword(ToonSsao.SsaoKeywordName));
                }

                if (!AnyExtension<ToonSsaoAsset>(ssao => ssao.Settings.Pattern != null))
                {
                    _keywordsToStrip.Add(new ShaderKeyword(ToonSsao.SsaoPatternKeywordName));
                }
            }

            // ToonRPInvertedHullOutline
            {
                if (!AnyExtension<ToonInvertedHullOutlineAsset>(e =>
                        e.Settings.Passes.Any(p => p.IsNoiseEnabled)
                    ))
                {
                    _localKeywordsToStrip.Add(
                        (ToonInvertedHullOutline.ShaderName, ToonInvertedHullOutline.NoiseKeywordName)
                    );
                }

                if (!AnyExtension<ToonInvertedHullOutlineAsset>(e =>
                        e.Settings.Passes.Any(p => p.IsDistanceFadeEnabled)
                    ))
                {
                    _localKeywordsToStrip.Add((ToonInvertedHullOutline.ShaderName,
                            ToonInvertedHullOutline.DistanceFadeKeywordName)
                    );
                }
            }

            // ToonRPScreenSpaceOutline
            {
                if (!AnyPostProcessingPass<ToonScreenSpaceOutlineAsset>(a => a.Settings.ColorFilter.Enabled))
                {
                    _localKeywordsToStrip.Add((ToonScreenSpaceOutlineImpl.ShaderName,
                            ToonScreenSpaceOutlineImpl.ColorKeywordName)
                    );
                }

                if (!AnyExtension<ToonScreenSpaceOutlineAfterOpaqueAsset>(a => a.Settings.DepthFilter.Enabled) &&
                    !AnyPostProcessingPass<ToonScreenSpaceOutlineAsset>(a => a.Settings.DepthFilter.Enabled))
                {
                    _localKeywordsToStrip.Add((ToonScreenSpaceOutlineImpl.ShaderName,
                            ToonScreenSpaceOutlineImpl.DepthKeywordName)
                    );
                }

                if (!AnyExtension<ToonScreenSpaceOutlineAfterOpaqueAsset>(a => a.Settings.NormalsFilter.Enabled) &&
                    !AnyPostProcessingPass<ToonScreenSpaceOutlineAsset>(a => a.Settings.NormalsFilter.Enabled))
                {
                    _localKeywordsToStrip.Add((ToonScreenSpaceOutlineImpl.ShaderName,
                            ToonScreenSpaceOutlineImpl.NormalsKeywordName)
                    );
                }

                if (!AnyExtension<ToonScreenSpaceOutlineAfterOpaqueAsset>(a => a.Settings.UseFog) &&
                    !AnyPostProcessingPass<ToonScreenSpaceOutlineAsset>(a => a.Settings.UseFog))
                {
                    _localKeywordsToStrip.Add((ToonScreenSpaceOutlineImpl.ShaderName,
                            ToonScreenSpaceOutlineImpl.UseFogKeywordName)
                    );
                }

                if (!AnyExtension<ToonScreenSpaceOutlineAfterOpaqueAsset>())
                {
                    _localKeywordsToStrip.Add((ToonScreenSpaceOutlineImpl.ShaderName,
                            ToonScreenSpaceOutlineImpl.AlphaBlendingKeywordName)
                    );
                }
            }

            // ToonRPPostProcessingStack
            {
                if (!AnyPostProcessingPass<ToonPostProcessingStackAsset>(s => s.Settings.Fxaa.Enabled))
                {
                    _localKeywordsToStrip.Add((ToonPostProcessingStack.ShaderName,
                            ToonPostProcessingStack.FxaaLowKeywordName)
                    );
                    _localKeywordsToStrip.Add((ToonPostProcessingStack.ShaderName,
                            ToonPostProcessingStack.FxaaHighKeywordName)
                    );
                }

                if (!AnyPostProcessingPass<ToonPostProcessingStackAsset>(s =>
                        s.Settings.Fxaa.Enabled && s.Settings.Fxaa.HighQuality
                    ))
                {
                    _localKeywordsToStrip.Add((ToonPostProcessingStack.ShaderName,
                            ToonPostProcessingStack.FxaaHighKeywordName)
                    );
                }

                if (!AnyPostProcessingPass<ToonPostProcessingStackAsset>(s =>
                        s.Settings.Fxaa.Enabled && !s.Settings.Fxaa.HighQuality
                    ))
                {
                    _localKeywordsToStrip.Add((ToonPostProcessingStack.ShaderName,
                            ToonPostProcessingStack.FxaaLowKeywordName)
                    );
                }

                if (!AnyPostProcessingPass<ToonPostProcessingStackAsset>(s => s.Settings.ToneMapping.Enabled))
                {
                    _localKeywordsToStrip.Add((ToonPostProcessingStack.ShaderName,
                            ToonPostProcessingStack.ToneMappingKeywordName)
                    );
                }

                if (!AnyPostProcessingPass<ToonPostProcessingStackAsset>(s => s.Settings.Vignette.Enabled))
                {
                    _localKeywordsToStrip.Add((ToonPostProcessingStack.ShaderName,
                            ToonPostProcessingStack.VignetteKeywordName)
                    );
                }

                if (!AnyPostProcessingPass<ToonPostProcessingStackAsset>(s => s.Settings.LookupTable.Enabled))
                {
                    _localKeywordsToStrip.Add((ToonPostProcessingStack.ShaderName,
                            ToonPostProcessingStack.LookupTableKeywordName)
                    );
                }

                if (!AnyPostProcessingPass<ToonPostProcessingStackAsset>(s => s.Settings.FilmGrain.Enabled))
                {
                    _localKeywordsToStrip.Add((ToonPostProcessingStack.ShaderName,
                            ToonPostProcessingStack.FilmGrainKeywordName)
                    );
                }
            }
        }

        public int callbackOrder => 0;

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            string logMessage = string.Empty;

            for (int i = 0; i < data.Count; i++)
            {
                ShaderCompilerData shaderCompilerData = data[i];
                if (!ShouldStrip(shader, shaderCompilerData))
                {
                    continue;
                }

                string keywords = string.Join(";", data[i].shaderKeywordSet.GetShaderKeywords());
                logMessage += $"Toon RP: stripping {shader.name} ({keywords}).\n";
                data.RemoveAt(i);
                --i;
            }

            if (!string.IsNullOrEmpty(logMessage))
            {
                Debug.Log(logMessage);
            }
        }

        private bool AnyExtension<TExtension>(Func<TExtension, bool> condition)
            where TExtension : ToonRenderingExtensionAsset =>
            _allToonRenderPipelineAssets.Any(a =>
                a.Extensions.Extensions.OfType<TExtension>().Any(condition)
            );

        private bool AnyExtension<TExtension>() where TExtension : ToonRenderingExtensionAsset =>
            _allToonRenderPipelineAssets.Any(a =>
                a.Extensions.Extensions.OfType<TExtension>().Any()
            );

        private bool AnyPostProcessingPass<TPass>(Func<TPass, bool> condition)
            where TPass : ToonPostProcessingPassAsset =>
            _allToonRenderPipelineAssets.Any(a =>
                a.PostProcessing.Passes.OfType<TPass>().Any(condition)
            );

        private bool ShouldStrip(Shader shader, ShaderCompilerData shaderCompilerData)
        {
            foreach (string shaderToStrip in _shadersToStrip)
            {
                if (shader.name == shaderToStrip)
                {
                    return true;
                }
            }

            foreach ((string shaderName, string keyword) in _localKeywordsToStrip)
            {
                if (shader.name == shaderName &&
                    shaderCompilerData.shaderKeywordSet.IsEnabled(new ShaderKeyword(shader, keyword)))
                {
                    return true;
                }
            }

            foreach (ShaderKeyword shaderKeyword in _keywordsToStrip)
            {
                if (shaderCompilerData.shaderKeywordSet.IsEnabled(shaderKeyword))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

#endif // UNITY_EDITOR || DEVELOPMENT_BUILD