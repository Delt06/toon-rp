using System;
using System.Collections.Generic;
using System.Linq;
using DELTation.ToonRP.Editor.GlobalSettings;
using DELTation.ToonRP.Extensions;
using DELTation.ToonRP.Extensions.BuiltIn;
using DELTation.ToonRP.Lighting;
using DELTation.ToonRP.PostProcessing;
using DELTation.ToonRP.PostProcessing.BuiltIn;
using DELTation.ToonRP.Shadows;
using DELTation.ToonRP.Shadows.Blobs;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using static DELTation.ToonRP.Shadows.ToonShadowMapsSettings;

namespace DELTation.ToonRP.Editor.Stripping
{
    [UsedImplicitly]
    public class ToonShaderBuildPreprocessor : IPreprocessShaders, IPreprocessComputeShaders
    {
        private readonly List<ToonRenderPipelineAsset> _allToonRenderPipelineAssets;
        private readonly HashSet<string> _computeShadersToStrip = new();
        private readonly List<ShaderKeyword> _keywordsToStrip = new();
        private readonly Dictionary<string, List<string>> _localKeywordsToStrip = new();
        private readonly HashSet<string> _shadersToStrip = new();

        public ToonShaderBuildPreprocessor()
        {
            var globalSettings = ToonRpGlobalSettings.GetOrCreateSettings();
            if (!ShouldStripAtAll(globalSettings))
            {
                return;
            }

            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;

            var renderPipelineAssets = new List<ToonRenderPipelineAsset>();
            if (!TryGetRenderPipelineAssetsForBuildTarget(target, renderPipelineAssets))
            {
                return;
            }

            _allToonRenderPipelineAssets = renderPipelineAssets
                .Where(a => a != null)
                .Distinct()
                .ToList();

            // Additional lights
            {
                if (_allToonRenderPipelineAssets.All(a =>
                        a.CameraRendererSettings.AdditionalLights !=
                        ToonCameraRendererSettings.AdditionalLightsMode.PerPixel
                    ))
                {
                    _keywordsToStrip.Add(new ShaderKeyword(ToonLighting.Keywords.AdditionalLights));
                }

                if (_allToonRenderPipelineAssets.All(a =>
                        a.CameraRendererSettings.AdditionalLights !=
                        ToonCameraRendererSettings.AdditionalLightsMode.PerVertex
                    ))
                {
                    _keywordsToStrip.Add(new ShaderKeyword(ToonLighting.Keywords.AdditionalLightsVertex));
                }
            }

            // Tiled lighting
            {
                if (_allToonRenderPipelineAssets.All(a => !a.CameraRendererSettings.IsTiledLightingEnabled))
                {
                    _computeShadersToStrip.Add(ToonTiledLighting.SetupComputeShaderName);
                    _computeShadersToStrip.Add(ToonTiledLighting.ComputeTileBoundsComputeShaderName);
                    _computeShadersToStrip.Add(ToonTiledLighting.CullLightsComputeShaderName);

                    _keywordsToStrip.Add(new ShaderKeyword(ToonTiledLighting.TiledLightingKeywordName));
                }
            }

            // Blob shadows
            if (_allToonRenderPipelineAssets.All(a => a.ShadowSettings.Mode != ToonShadowSettings.ShadowMode.Blobs))
            {
                _keywordsToStrip.Add(new ShaderKeyword(ToonShadows.BlobShadowsKeywordName));
                _shadersToStrip.Add(ToonBlobShadows.ShaderName);
            }

            // VSM
            if (_allToonRenderPipelineAssets.All(a =>
                    a.ShadowSettings.Mode != ToonShadowSettings.ShadowMode.ShadowMapping
                ))
            {
                _keywordsToStrip.Add(new ShaderKeyword(ToonShadows.DirectionalShadowsKeywordName));
                _keywordsToStrip.Add(new ShaderKeyword(ToonShadows.DirectionalCascadedShadowsKeywordName));
            }

            // Additional Lights Shadows
            if (_allToonRenderPipelineAssets.All(a =>
                    a.CameraRendererSettings.AdditionalLights != ToonCameraRendererSettings.AdditionalLightsMode.PerPixel ||
                    !a.ShadowSettings.ShadowMaps.Additional.Enabled
                ))
            {
                _keywordsToStrip.Add(new ShaderKeyword(ToonShadows.AdditionalShadowsKeywordName));
            }

            if (!_allToonRenderPipelineAssets.Any(a =>
                    a.ShadowSettings.Mode == ToonShadowSettings.ShadowMode.ShadowMapping &&
                    a.ShadowSettings.ShadowMaps.Blur != BlurMode.None
                ))
            {
                _keywordsToStrip.Add(new ShaderKeyword(ToonShadows.VsmKeywordName));
            }

            // Soft Shadows (Poisson PCF)
            {
                bool AnyPipelineAssetHasSoftShadows([CanBeNull] Predicate<SoftShadowsSettings> extraCondition = null) =>
                    _allToonRenderPipelineAssets.Any(a => a.ShadowSettings is
                        {
                            Mode: ToonShadowSettings.ShadowMode.ShadowMapping, ShadowMaps:
                            {
                                Blur: BlurMode.None,
                                SoftShadows:
                                {
                                    Enabled: true,
                                },
                            },
                        } && (extraCondition?.Invoke(a.ShadowSettings.ShadowMaps.SoftShadows) ?? true)
                    );

                if (!AnyPipelineAssetHasSoftShadows())
                {
                    _keywordsToStrip.Add(new ShaderKeyword(ToonShadows.PcfKeywordName));
                }

                if (!AnyPipelineAssetHasSoftShadows(s => s is { Mode : SoftShadowsMode.PoissonStratified }))
                {
                    _keywordsToStrip.Add(new ShaderKeyword(ToonShadows.PoissonStratifiedKeywordName));
                }

                if (!AnyPipelineAssetHasSoftShadows(s => s is { Mode : SoftShadowsMode.PoissonRotated }))
                {
                    _keywordsToStrip.Add(new ShaderKeyword(ToonShadows.PoissonRotatedKeywordName));
                }

                if (!AnyPipelineAssetHasSoftShadows(s => s is { Quality : SoftShadowsQuality.High, EarlyBail: true }))
                {
                    _keywordsToStrip.Add(new ShaderKeyword(ToonShadows.PoissonEarlyBailKeywordName));
                }
            }


            // ToonRPVsmBlur
            {
                if (!_allToonRenderPipelineAssets.Any(a =>
                        a.ShadowSettings.Mode == ToonShadowSettings.ShadowMode.ShadowMapping &&
                        a.ShadowSettings.ShadowMaps.Blur !=
                        BlurMode.None
                    ))
                {
                    _shadersToStrip.Add(ToonShadowMaps.BlurShaderName);
                }

                if (!_allToonRenderPipelineAssets.Any(a =>
                        a.ShadowSettings.Mode == ToonShadowSettings.ShadowMode.ShadowMapping &&
                        a.ShadowSettings.ShadowMaps.Blur ==
                        BlurMode.GaussianHighQuality
                    ))
                {
                    AddLocalKeywordToStrip(ToonShadowMaps.BlurShaderName, ToonShadowMaps.BlurHighQualityKeywordName);
                }

                if (!_allToonRenderPipelineAssets.Any(a =>
                        a.ShadowSettings.Mode == ToonShadowSettings.ShadowMode.ShadowMapping &&
                        a.ShadowSettings.ShadowMaps.IsBlurEarlyBailEnabled
                    ))
                {
                    AddLocalKeywordToStrip(ToonShadowMaps.BlurShaderName, ToonShadowMaps.BlurEarlyBailKeywordName);
                }
            }

            // VSM without cascades
            if (!_allToonRenderPipelineAssets.Any(a =>
                    a.ShadowSettings.Mode == ToonShadowSettings.ShadowMode.ShadowMapping &&
                    a.ShadowSettings.ShadowMaps.Directional.Enabled &&
                    a.ShadowSettings.ShadowMaps.Directional.CascadeCount == 1
                ))
            {
                _keywordsToStrip.Add(new ShaderKeyword(ToonShadows.DirectionalShadowsKeywordName));
            }

            // VSM with cascades
            if (!_allToonRenderPipelineAssets.Any(a =>
                    a.ShadowSettings.Mode == ToonShadowSettings.ShadowMode.ShadowMapping &&
                    a.ShadowSettings.ShadowMaps.Directional.Enabled &&
                    a.ShadowSettings.ShadowMaps.Directional.CascadeCount > 1
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

            // Baked Lighting
            {
                if (_allToonRenderPipelineAssets.All(a => (a.CameraRendererSettings.BakedLightingFeatures & ToonRpBakedLightingFeatures.LightMaps) == 0))
                {
                    _shadersToStrip.Add(ToonLighting.Keywords.LightmapShadowMixing);
                    _shadersToStrip.Add(ToonLighting.Keywords.BuiltIn.LightmapOn);
                    _shadersToStrip.Add(ToonLighting.Keywords.BuiltIn.DirLightmapCombined);
                }

                if (_allToonRenderPipelineAssets.All(a => (a.CameraRendererSettings.BakedLightingFeatures & ToonRpBakedLightingFeatures.ShadowMask) == 0))
                {
                    _shadersToStrip.Add(ToonLighting.Keywords.ShadowsShadowMask);
                }
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
                        e.Settings.Passes.Any(p => p.FixedScreenSpaceThickness)
                    ))
                {
                    AddLocalKeywordToStrip(ToonInvertedHullOutline.DefaultShaderName,
                        ToonInvertedHullOutline.ShaderKeywords.FixedScreenSpaceThicknessKeywordName
                    );
                }

                if (!AnyExtension<ToonInvertedHullOutlineAsset>(e =>
                        e.Settings.Passes.Any(p => p.IsNoiseEnabled)
                    ))
                {
                    AddLocalKeywordToStrip(ToonInvertedHullOutline.DefaultShaderName,
                        ToonInvertedHullOutline.ShaderKeywords.NoiseKeywordName
                    );
                }

                if (!AnyExtension<ToonInvertedHullOutlineAsset>(e =>
                        e.Settings.Passes.Any(p => p.IsDistanceFadeEnabled)
                    ))
                {
                    AddLocalKeywordToStrip(ToonInvertedHullOutline.DefaultShaderName,
                        ToonInvertedHullOutline.ShaderKeywords.DistanceFadeKeywordName
                    );
                }

                bool AnyExtensionHasNormalSource(
                    ToonInvertedHullOutlineSettings.NormalsSource normalsSource) =>
                    AnyExtension<ToonInvertedHullOutlineAsset>(e =>
                        e.Settings.Passes.Any(p =>
                            p.NormalsSource == normalsSource
                        )
                    );

                if (!AnyExtensionHasNormalSource(
                        ToonInvertedHullOutlineSettings.NormalsSource.UV2
                    ))
                {
                    AddLocalKeywordToStrip(ToonInvertedHullOutline.DefaultShaderName,
                        ToonInvertedHullOutline.ShaderKeywords.NormalSemanticUV2KeywordName
                    );
                }

                if (!AnyExtensionHasNormalSource(
                        ToonInvertedHullOutlineSettings.NormalsSource.Tangents
                    ))
                {
                    AddLocalKeywordToStrip(ToonInvertedHullOutline.DefaultShaderName,
                        ToonInvertedHullOutline.ShaderKeywords.NormalSemanticTangentKeywordName
                    );
                }
            }

            // ToonRPScreenSpaceOutline
            {
                if (!AnyPostProcessingPass<ToonScreenSpaceOutlineAsset>(a => a.Settings.ColorFilter.Enabled))
                {
                    AddLocalKeywordToStrip(ToonScreenSpaceOutlineImpl.ShaderName,
                        ToonScreenSpaceOutlineImpl.ColorKeywordName
                    );
                }

                if (!AnyExtension<ToonScreenSpaceOutlineAfterOpaqueAsset>(a => a.Settings.DepthFilter.Enabled) &&
                    !AnyPostProcessingPass<ToonScreenSpaceOutlineAsset>(a => a.Settings.DepthFilter.Enabled))
                {
                    AddLocalKeywordToStrip(ToonScreenSpaceOutlineImpl.ShaderName,
                        ToonScreenSpaceOutlineImpl.DepthKeywordName
                    );
                }

                if (!AnyExtension<ToonScreenSpaceOutlineAfterOpaqueAsset>(a => a.Settings.NormalsFilter.Enabled) &&
                    !AnyPostProcessingPass<ToonScreenSpaceOutlineAsset>(a => a.Settings.NormalsFilter.Enabled))
                {
                    AddLocalKeywordToStrip(ToonScreenSpaceOutlineImpl.ShaderName,
                        ToonScreenSpaceOutlineImpl.NormalsKeywordName
                    );
                }

                if (!AnyExtension<ToonScreenSpaceOutlineAfterOpaqueAsset>(a => a.Settings.UseFog) &&
                    !AnyPostProcessingPass<ToonScreenSpaceOutlineAsset>(a => a.Settings.UseFog))
                {
                    AddLocalKeywordToStrip(ToonScreenSpaceOutlineImpl.ShaderName,
                        ToonScreenSpaceOutlineImpl.UseFogKeywordName
                    );
                }

                if (!AnyExtension<ToonScreenSpaceOutlineAfterOpaqueAsset>())
                {
                    AddLocalKeywordToStrip(ToonScreenSpaceOutlineImpl.ShaderName,
                        ToonScreenSpaceOutlineImpl.AlphaBlendingKeywordName
                    );
                }
            }

            // ToonRPDebugPass
            {
                if (!AnyPostProcessingPass<ToonDebugPassAsset>(a => a.Settings.IsEffectivelyEnabled()))
                {
                    _shadersToStrip.Add(ToonDebugPass.ShaderName);
                }
            }

            // ToonRPPostProcessingStack
            {
                if (!AnyPostProcessingPass<ToonPostProcessingStackAsset>(s => s.Settings.Fxaa.Enabled))
                {
                    AddLocalKeywordToStrip(ToonPostProcessingStack.ShaderName,
                        ToonPostProcessingStack.FxaaLowKeywordName
                    );
                    AddLocalKeywordToStrip(ToonPostProcessingStack.ShaderName,
                        ToonPostProcessingStack.FxaaHighKeywordName
                    );
                }

                if (!AnyPostProcessingPass<ToonPostProcessingStackAsset>(s =>
                        s.Settings.Fxaa.Enabled && s.Settings.Fxaa.HighQuality
                    ))
                {
                    AddLocalKeywordToStrip(ToonPostProcessingStack.ShaderName,
                        ToonPostProcessingStack.FxaaHighKeywordName
                    );
                }

                if (!AnyPostProcessingPass<ToonPostProcessingStackAsset>(s =>
                        s.Settings.Fxaa.Enabled && !s.Settings.Fxaa.HighQuality
                    ))
                {
                    AddLocalKeywordToStrip(ToonPostProcessingStack.ShaderName,
                        ToonPostProcessingStack.FxaaLowKeywordName
                    );
                }

                if (!AnyPostProcessingPass<ToonPostProcessingStackAsset>(s => s.Settings.ToneMapping.Enabled))
                {
                    AddLocalKeywordToStrip(ToonPostProcessingStack.ShaderName,
                        ToonPostProcessingStack.ToneMappingKeywordName
                    );
                }

                if (!AnyPostProcessingPass<ToonPostProcessingStackAsset>(s => s.Settings.Vignette.Enabled))
                {
                    AddLocalKeywordToStrip(ToonPostProcessingStack.ShaderName,
                        ToonPostProcessingStack.VignetteKeywordName
                    );
                }

                if (!AnyPostProcessingPass<ToonPostProcessingStackAsset>(s => s.Settings.LookupTable.Enabled))
                {
                    AddLocalKeywordToStrip(ToonPostProcessingStack.ShaderName,
                        ToonPostProcessingStack.LookupTableKeywordName
                    );
                }

                if (!AnyPostProcessingPass<ToonPostProcessingStackAsset>(s => s.Settings.FilmGrain.Enabled))
                {
                    AddLocalKeywordToStrip(ToonPostProcessingStack.ShaderName,
                        ToonPostProcessingStack.FilmGrainKeywordName
                    );
                }
            }

            // ToonRPDepthDownsample
            {
                if (!AnyExtension<ToonOffScreenTransparencyAsset>(t =>
                        t.Settings.DepthMode == ToonOffScreenTransparencySettings.DepthRenderMode.Downsample
                    ))
                {
                    _shadersToStrip.Add(ToonDepthDownsample.ShaderName);
                }

                if (!AnyExtension<ToonOffScreenTransparencyAsset>(t =>
                        t.Settings.DepthMode == ToonOffScreenTransparencySettings.DepthRenderMode.Downsample &&
                        t.Settings.DepthDownsampleQuality ==
                        ToonOffScreenTransparencySettings.DepthDownsampleQualityLevel.High
                    ))
                {
                    AddLocalKeywordToStrip(ToonDepthDownsample.ShaderName, ToonDepthDownsample.HighQualityKeyword);
                }
            }

            // ToonTemporalAA
            {
                if (!AnyPostProcessingPass<ToonTemporalAAAsset>())
                {
                    _shadersToStrip.Add(ToonTemporalAA.ShaderName);
                }
            }

            // ToonSharpen
            {
                if (!AnyPostProcessingPass<ToonSharpenAsset>())
                {
                    _shadersToStrip.Add(ToonSharpen.ShaderName);
                }
            }

#if !(ENABLE_VR && ENABLE_XR_MODULE)
            _shadersToStrip.Add(DELTation.ToonRP.Xr.ToonXr.OcclusionMeshShaderName);
            _shadersToStrip.Add(DELTation.ToonRP.Xr.ToonXr.MirrorViewShaderName);
            _shadersToStrip.Add(ToonCopyDepth.ShaderName);
#endif // !(ENABLE_VR && ENABLE_XR_MODULE)

            ReportStrippingConfiguration();
        }

        public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data)
        {
            for (int i = 0; i < data.Count; i++)
            {
                if (!ShouldStripComputeShader(shader))
                {
                    continue;
                }

                FastRemoveAt(data, i);
                --i;
            }
        }

        public int callbackOrder => 0;

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            for (int i = 0; i < data.Count; i++)
            {
                ShaderCompilerData shaderCompilerData = data[i];
                if (!ShouldStripShader(shader, ref shaderCompilerData))
                {
                    continue;
                }

                FastRemoveAt(data, i);
                --i;
            }
        }

        private static void FastRemoveAt<T>(IList<T> list, int index)
        {
            int lastIndex = list.Count - 1;
            if (index == lastIndex)
            {
                list.RemoveAt(index);
                return;
            }

            // move the deleted item to the end then remove it
            (list[lastIndex], list[index]) = (list[index], list[lastIndex]);
            list.RemoveAt(lastIndex);
        }

        private void AddLocalKeywordToStrip(string shaderName, string keyword)
        {
            if (!_localKeywordsToStrip.TryGetValue(shaderName, out List<string> keywords))
            {
                _localKeywordsToStrip[shaderName] = keywords = new List<string>();
            }

            keywords.Add(keyword);
        }

        private bool ShouldStripComputeShader(ComputeShader computeShader) =>
            _computeShadersToStrip.Contains(computeShader.name);

        private void ReportStrippingConfiguration()
        {
            string separator = Environment.NewLine;

            {
                string shadersToStripString = string.Join(separator, _shadersToStrip);
                Debug.Log($"Toon RP: stripping shaders: {shadersToStripString}");
            }

            {
                string shadersToStripString = string.Join(separator, _computeShadersToStrip);
                Debug.Log($"Toon RP: stripping compute shaders: {shadersToStripString}");
            }

            {
                string globalKeywordsToStripString = string.Join(separator, _keywordsToStrip);
                Debug.Log($"Toon RP: stripping global shader keywords: {globalKeywordsToStripString}");
            }

            {
                IEnumerable<string> localKeywords =
                    _localKeywordsToStrip.SelectMany(kvp =>
                        kvp.Value.Select(v => $"{kvp.Key} ({v})")
                    );
                string localKeywordsToStripString = string.Join(separator, localKeywords);
                Debug.Log($"Toon RP: stripping local shader keywords: {localKeywordsToStripString}");
            }
        }

        private static bool ShouldStripAtAll(ToonRpGlobalSettings globalSettings) =>
            globalSettings.ShaderVariantStrippingMode switch
            {
                ShaderVariantStrippingMode.Always => true,
                ShaderVariantStrippingMode.Never => false,
                ShaderVariantStrippingMode.OnlyInRelease => !EditorUserBuildSettings.development,
                _ => throw new ArgumentOutOfRangeException(),
            };

        private static bool TryGetRenderPipelineAssetsForBuildTarget(BuildTarget buildTarget,
            List<ToonRenderPipelineAsset> pipelineAssets)
        {
            using var qualitySettings = new SerializedObject(QualitySettings.GetQualitySettings());

            SerializedProperty property = qualitySettings.FindProperty("m_QualitySettings");
            if (property == null)
            {
                return false;
            }

            BuildTargetGroup activeBuildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            string activeBuildTargetGroupName = activeBuildTargetGroup.ToString();

            bool allQualityLevelsAreOverridden = true;
            for (int i = 0; i < property.arraySize; i++)
            {
                bool isExcluded = false;

                SerializedProperty excludedTargetPlatforms =
                    property.GetArrayElementAtIndex(i).FindPropertyRelative("excludedTargetPlatforms");
                if (excludedTargetPlatforms == null)
                {
                    return false;
                }

                foreach (SerializedProperty excludedTargetPlatform in excludedTargetPlatforms)
                {
                    string excludedBuildTargetGroupName = excludedTargetPlatform.stringValue;
                    if (activeBuildTargetGroupName == excludedBuildTargetGroupName)
                    {
                        Debug.Log($"Excluding quality level {QualitySettings.names[i]} from stripping.");
                        isExcluded = true;
                        break;
                    }
                }

                if (!isExcluded)
                {
                    if (QualitySettings.GetRenderPipelineAssetAt(i) is ToonRenderPipelineAsset pipelineAsset)
                    {
                        pipelineAssets.Add(pipelineAsset);
                    }
                    else
                    {
                        allQualityLevelsAreOverridden = false;
                    }
                }
            }

            if (!allQualityLevelsAreOverridden || pipelineAssets.Count == 0)
            {
                if (GraphicsSettings.defaultRenderPipeline is ToonRenderPipelineAsset pipelineAsset)
                {
                    pipelineAssets.Add(pipelineAsset);
                }
            }

            return true;
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

        private bool AnyPostProcessingPass<TPass>()
            where TPass : ToonPostProcessingPassAsset =>
            _allToonRenderPipelineAssets.Any(a =>
                a.PostProcessing.Passes.OfType<TPass>().Any()
            );

        private bool ShouldStripShader(Shader shader, ref ShaderCompilerData shaderCompilerData)
        {
            if (_shadersToStrip.Contains(shader.name))
            {
                return true;
            }

            if (_localKeywordsToStrip.TryGetValue(shader.name, out List<string> localKeywords))
            {
                foreach (string keyword in localKeywords)
                {
                    if (shaderCompilerData.shaderKeywordSet.IsEnabled(new ShaderKeyword(shader, keyword)))
                    {
                        return true;
                    }
                }
            }

            foreach (ShaderKeyword keyword in _keywordsToStrip)
            {
                if (shaderCompilerData.shaderKeywordSet.IsEnabled(keyword))
                {
                    return true;
                }
            }

            return false;
        }
    }
}