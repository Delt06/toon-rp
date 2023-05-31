using System;
using System.IO;
using DELTation.ToonRP.Editor.ShaderGUI;
using DELTation.ToonRP.Editor.ShaderGUI.ShaderEnums;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;
using UnityBlendMode = UnityEngine.Rendering.BlendMode;

namespace DELTation.ToonRP.Editor.AssetPostProcessors
{
    // For reference, see the analogous class in URP
    internal class FBXMaterialDescriptionPreprocessor : AssetPostprocessor
    {
        private const uint Version = 1;
        private const int Order = -981;

        public void OnPreprocessMaterialDescription(MaterialDescription description, Material material,
            AnimationClip[] clips)
        {
            if (material == null)
            {
                return;
            }

            RenderPipelineAsset pipelineAsset = GraphicsSettings.currentRenderPipeline;
            if (!pipelineAsset || pipelineAsset.GetType() != typeof(ToonRenderPipelineAsset))
            {
                return;
            }

            string lowerCaseExtension = Path.GetExtension(assetPath).ToLower();
            if (lowerCaseExtension != ".fbx" && lowerCaseExtension != ".obj" && lowerCaseExtension != ".blend" &&
                lowerCaseExtension != ".mb" && lowerCaseExtension != ".ma" && lowerCaseExtension != ".max")
            {
                return;
            }

            Shader shader = ToonRenderPipeline.GetDefaultShader();
            if (shader == null)
            {
                return;
            }

            material.shader = shader;

            Vector4 vectorProperty;
            float floatProperty;

            bool isTransparent = false;

            if (!description.TryGetProperty("Opacity", out float opacity))
            {
                if (description.TryGetProperty("TransparencyFactor", out float transparencyFactor))
                {
                    opacity = Mathf.Approximately(transparencyFactor, 1.0f) ? 1.0f : 1.0f - transparencyFactor;
                }

                if (Mathf.Approximately(opacity, 1.0f) &&
                    description.TryGetProperty("TransparentColor", out vectorProperty))
                {
                    opacity = Mathf.Approximately(vectorProperty.x, 1.0f) ? 1.0f : 1.0f - vectorProperty.x;
                }
            }

            if (opacity < 1.0f ||
                Mathf.Approximately(opacity, 1.0f) &&
                description.TryGetProperty("TransparentColor", out TexturePropertyDescription textureProperty))
            {
                isTransparent = true;
            }
            else if (description.HasAnimationCurve("TransparencyFactor") ||
                     description.HasAnimationCurve("TransparentColor"))
            {
                isTransparent = true;
            }

            if (isTransparent)
            {
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetFloat(PropertyNames.BlendDst, (float) UnityBlendMode.One);
                material.SetFloat(PropertyNames.BlendDst, (float) UnityBlendMode.OneMinusSrcAlpha);
                material.SetFloat(PropertyNames.ZWrite, 0.0f);
                material.EnableKeyword(ShaderKeywords.AlphaPremultiplyOn);
                material.renderQueue = (int) RenderQueue.Transparent;
                material.SetFloat(PropertyNames.SurfaceType, (float) SurfaceType.Transparent);
            }
            else
            {
                material.SetOverrideTag("RenderType", "");
                material.SetFloat(PropertyNames.BlendSrc, (float) UnityBlendMode.One);
                material.SetFloat(PropertyNames.BlendDst, (float) UnityBlendMode.Zero);
                material.SetFloat(PropertyNames.ZWrite, 1.0f);
                material.SetFloat(PropertyNames.AlphaClipping, 0);
                material.DisableKeyword(ShaderKeywords.AlphaTestOn);
                material.DisableKeyword(ShaderKeywords.AlphaPremultiplyOn);
                material.renderQueue = -1;
                material.SetFloat(PropertyNames.SurfaceType, (float) SurfaceType.Opaque);
            }

            if (description.TryGetProperty("DiffuseColor", out textureProperty) &&
                textureProperty.texture != null)
            {
                var diffuseColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                if (description.TryGetProperty("DiffuseFactor", out floatProperty))
                {
                    diffuseColor *= floatProperty;
                }

                diffuseColor.a = opacity;

                SetMaterialTextureProperty(PropertyNames.MainTexture, material, textureProperty);
                material.SetColor(PropertyNames.MainColor, diffuseColor);
            }
            else if (description.TryGetProperty("DiffuseColor", out vectorProperty))
            {
                Color diffuseColor = vectorProperty;
                diffuseColor.a = opacity;
                material.SetColor(PropertyNames.MainColor,
                    PlayerSettings.colorSpace == ColorSpace.Linear ? diffuseColor.gamma : diffuseColor
                );
            }

            if (description.TryGetProperty("Bump", out textureProperty))
            {
                SetMaterialTextureProperty(PropertyNames.NormalMapPropertyName, material, textureProperty);
                material.EnableKeyword(ShaderKeywords.NormalMap);
            }
            else if (description.TryGetProperty("NormalMap", out textureProperty))
            {
                SetMaterialTextureProperty(PropertyNames.NormalMapPropertyName, material, textureProperty);
                material.EnableKeyword(ShaderKeywords.NormalMap);
            }

            if (
                description.TryGetProperty("EmissiveColor", out vectorProperty) &&
                vectorProperty.magnitude > vectorProperty.w
                || description.HasAnimationCurve("EmissiveColor.x"))
            {
                if (description.TryGetProperty("EmissiveFactor", out floatProperty))
                {
                    vectorProperty *= floatProperty;
                }

                material.SetColor(PropertyNames.EmissionColor, vectorProperty);
            }

            if (PlayerSettings.colorSpace == ColorSpace.Linear)
            {
                RemapAndTransformColorCurves(description, clips, "DiffuseColor", PropertyNames.MainColor,
                    ConvertFloatLinearToGamma
                );
            }
            else
            {
                RemapColorCurves(description, clips, "DiffuseColor", PropertyNames.MainColor);
            }

            RemapTransparencyCurves(description, clips);

            RemapColorCurves(description, clips, "EmissiveColor", PropertyNames.MainColor);
        }

        public override uint GetVersion() => Version;

        public override int GetPostprocessOrder() => Order;

        private static void RemapTransparencyCurves(MaterialDescription description, AnimationClip[] clips)
        {
            // For some reason, Opacity is never animated, we have to use TransparencyFactor and TransparentColor
            foreach (AnimationClip clip in clips)
            {
                bool foundTransparencyCurve = false;
                if (description.TryGetAnimationCurve(clip.name, "TransparencyFactor", out AnimationCurve curve))
                {
                    ConvertKeys(curve, ConvertFloatOneMinus);
                    clip.SetCurve("", typeof(Material), PropertyNames.MainColor + ".a", curve);
                    foundTransparencyCurve = true;
                }
                else if (description.TryGetAnimationCurve(clip.name, "TransparentColor.x", out curve))
                {
                    ConvertKeys(curve, ConvertFloatOneMinus);
                    clip.SetCurve("", typeof(Material), PropertyNames.MainColor + ".a", curve);
                    foundTransparencyCurve = true;
                }

                if (foundTransparencyCurve && !description.HasAnimationCurveInClip(clip.name, "DiffuseColor"))
                {
                    description.TryGetProperty("DiffuseColor", out Vector4 diffuseColor);
                    clip.SetCurve("", typeof(Material), PropertyNames.MainColor + ".r",
                        AnimationCurve.Constant(0.0f, 1.0f, diffuseColor.x)
                    );
                    clip.SetCurve("", typeof(Material), PropertyNames.MainColor + ".g",
                        AnimationCurve.Constant(0.0f, 1.0f, diffuseColor.y)
                    );
                    clip.SetCurve("", typeof(Material), PropertyNames.MainColor + ".b",
                        AnimationCurve.Constant(0.0f, 1.0f, diffuseColor.z)
                    );
                }
            }
        }

        private static void RemapColorCurves(MaterialDescription description, AnimationClip[] clips,
            string originalPropertyName, string newPropertyName)
        {
            foreach (AnimationClip clip in clips)
            {
                if (description.TryGetAnimationCurve(clip.name, originalPropertyName + ".x", out AnimationCurve curve))
                {
                    clip.SetCurve("", typeof(Material), newPropertyName + ".r", curve);
                }

                if (description.TryGetAnimationCurve(clip.name, originalPropertyName + ".y", out curve))
                {
                    clip.SetCurve("", typeof(Material), newPropertyName + ".g", curve);
                }

                if (description.TryGetAnimationCurve(clip.name, originalPropertyName + ".z", out curve))
                {
                    clip.SetCurve("", typeof(Material), newPropertyName + ".b", curve);
                }
            }
        }

        private static void RemapAndTransformColorCurves(MaterialDescription description, AnimationClip[] clips,
            string originalPropertyName, string newPropertyName, Func<float, float> converter)
        {
            foreach (AnimationClip clip in clips)
            {
                if (description.TryGetAnimationCurve(clip.name, originalPropertyName + ".x", out AnimationCurve curve))
                {
                    ConvertKeys(curve, converter);
                    clip.SetCurve("", typeof(Material), newPropertyName + ".r", curve);
                }

                if (description.TryGetAnimationCurve(clip.name, originalPropertyName + ".y", out curve))
                {
                    ConvertKeys(curve, converter);
                    clip.SetCurve("", typeof(Material), newPropertyName + ".g", curve);
                }

                if (description.TryGetAnimationCurve(clip.name, originalPropertyName + ".z", out curve))
                {
                    ConvertKeys(curve, converter);
                    clip.SetCurve("", typeof(Material), newPropertyName + ".b", curve);
                }
            }
        }

        private static float ConvertFloatLinearToGamma(float value) => Mathf.LinearToGammaSpace(value);

        private static float ConvertFloatOneMinus(float value) => 1.0f - value;

        private static void ConvertKeys(AnimationCurve curve, Func<float, float> conversionDelegate)
        {
            Keyframe[] keyframes = curve.keys;
            for (int i = 0; i < keyframes.Length; i++)
            {
                keyframes[i].value = conversionDelegate(keyframes[i].value);
            }

            curve.keys = keyframes;
        }

        private static void SetMaterialTextureProperty(string propertyName, Material material,
            TexturePropertyDescription textureProperty)
        {
            material.SetTexture(propertyName, textureProperty.texture);
            material.SetTextureOffset(propertyName, textureProperty.offset);
            material.SetTextureScale(propertyName, textureProperty.scale);
        }
    }
}