using System;
using JetBrains.Annotations;
using ToonRP.Editor.ShaderGUI.ShaderEnums;
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
            var surfaceTypeValue = (SurfaceType) surfaceType.floatValue;
            if (surfaceTypeValue == SurfaceType.Transparent)
            {
                if (surfaceTypeChanged)
                {
                    SetZWrite(false);
                }

                if (DrawProperty(PropertyNames.BlendMode, out MaterialProperty blendMode))
                {
                    (UnityBlendMode blendSrc, UnityBlendMode blendDst) = (BlendMode) blendMode.floatValue switch
                    {
                        BlendMode.Alpha => (UnityBlendMode.SrcAlpha, UnityBlendMode.OneMinusSrcAlpha),
                        BlendMode.Premultiply => (UnityBlendMode.One, UnityBlendMode.OneMinusSrcAlpha),
                        BlendMode.Additive => (UnityBlendMode.One, UnityBlendMode.One),
                        BlendMode.Multiply => (UnityBlendMode.DstColor, UnityBlendMode.Zero),
                        _ => throw new ArgumentOutOfRangeException(),
                    };
                    SetBlend(blendSrc, blendDst);
                }
            }
            else if (surfaceTypeChanged)
            {
                SetBlend(UnityBlendMode.One, UnityBlendMode.Zero);
                SetZWrite(true);
            }

            DrawProperty(PropertyNames.RenderFace);

            EditorGUILayout.Space();

            DrawProperty(PropertyNames.MainColor);
            DrawProperty(PropertyNames.MainTexture);
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
                SurfaceType.Opaque => RenderQueue.Geometry,
                SurfaceType.Transparent => RenderQueue.Transparent,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
    }
}