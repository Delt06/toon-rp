using System;
using JetBrains.Annotations;
using ToonRP.Editor.ShaderGUI.ShaderEnums;
using ToonRP.Runtime;
using UnityEditor;
using UnityEngine.Rendering;
using BlendMode = ToonRP.Editor.ShaderGUI.ShaderEnums.BlendMode;
using UnityBlendMode = UnityEngine.Rendering.BlendMode;

namespace ToonRP.Editor.ShaderGUI
{
    [UsedImplicitly]
    public sealed class ToonRpParticlesUnlitShaderGui : ToonRpShaderGuiBase
    {
        protected override void DrawProperties()
        {
            bool surfaceTypeChanged = DrawProperty(PropertyNames.SurfaceType, out MaterialProperty surfaceType);
            DrawAlphaClipping();
            var surfaceTypeValue = (SurfaceType) surfaceType.floatValue;
            if (surfaceTypeValue == SurfaceType.Transparent)
            {
                if (surfaceTypeChanged)
                {
                    SetZWrite(false);
                }

                if (DrawProperty(PropertyNames.BlendMode, out MaterialProperty blendMode) || surfaceTypeChanged)
                {
                    var blendModeValue = (BlendMode) blendMode.floatValue;
                    (UnityBlendMode blendSrc, UnityBlendMode blendDst) = blendModeValue switch
                    {
                        BlendMode.Alpha => (UnityBlendMode.SrcAlpha, UnityBlendMode.OneMinusSrcAlpha),
                        BlendMode.Premultiply => (UnityBlendMode.One, UnityBlendMode.OneMinusSrcAlpha),
                        BlendMode.Additive => (UnityBlendMode.One, UnityBlendMode.One),
                        BlendMode.Multiply => (UnityBlendMode.DstColor, UnityBlendMode.Zero),
                        _ => throw new ArgumentOutOfRangeException(),
                    };
                    SetBlend(blendSrc, blendDst);

                    Material.SetKeyword(ShaderKeywords.AlphaPremultiplyOn, blendModeValue == BlendMode.Premultiply);
                }
            }
            else if (surfaceTypeChanged)
            {
                SetBlend(UnityBlendMode.One, UnityBlendMode.Zero);
                SetZWrite(true);
                Material.DisableKeyword(ShaderKeywords.AlphaPremultiplyOn);
            }

            DrawProperty(PropertyNames.RenderFace);

            EditorGUILayout.Space();

            DrawProperty(PropertyNames.MainColor);
            DrawProperty(PropertyNames.MainTexture);
            DrawProperty(PropertyNames.EmissionColor);
        }

        private void SetBlend(UnityBlendMode blendSrc, UnityBlendMode blendDst)
        {
            Material.SetFloat(PropertyNames.BlendSrc, (float) blendSrc);
            Material.SetFloat(PropertyNames.BlendDst, (float) blendDst);
        }

        private void SetZWrite(bool zWrite)
        {
            Material.SetFloat(PropertyNames.ZWrite, zWrite ? 1.0f : 0.0f);
        }

        protected override RenderQueue GetRenderQueue()
        {
            MaterialProperty surfaceType = FindProperty(PropertyNames.SurfaceType);
            return (SurfaceType) surfaceType.floatValue switch
            {
                SurfaceType.Opaque when AlphaClippingEnabled() => RenderQueue.AlphaTest,
                SurfaceType.Opaque => RenderQueue.Geometry,
                SurfaceType.Transparent => RenderQueue.Transparent,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
    }
}