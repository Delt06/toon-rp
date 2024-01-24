using System.Collections.Generic;
using DELTation.ToonRP.Attributes;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Editor.BlobShadowBake
{
    [ScriptedImporter(1, Extension)]
    public class BakedBlobShadowImporter : ScriptedImporter
    {
        public const string Extension = "bakedblobshadow";
        private const string BlurShaderPath =
            "Packages/com.deltation.toon-rp/Shaders/Shadows/ToonRPBakedBlobShadowBlur.shader";

        private static readonly List<Material> Materials = new();
        private static readonly List<Renderer> Renderers = new();
        private static readonly int ApplyStepToSourceSamplesId = Shader.PropertyToID("_ApplyStepToSourceSamples");
        private static readonly int DirectionId = Shader.PropertyToID("_Direction");
        [Min(4)]
        public int Width = 32;
        [Min(4)]
        public int Height = 32;

        public GameObject Model;
        public bool IncludeSkinnedRenderers;
        public Vector2 ModelPositionOffset;
        [Min(0.0f)]
        public float BoundsSize = 5.0f;

        [Min(0)]
        public int BlurIterations = 4;

        public bool GenerateMipMaps = true;
        public bool ReadWriteEnabled;

        [ToonRpHeader("Compression")]
        public bool Compressed;
        [ToonRpShowIf(nameof(Compressed))]
        public bool HighQualityCompression;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            if (Model == null)
            {
                return;
            }

            if (!PrefabUtility.IsPartOfPrefabAsset(Model))
            {
                ctx.LogImportError($"{Model} is not a prefab asset.");
                return;
            }

            ctx.DependsOnArtifact(AssetDatabase.GetAssetPath(Model));
            ctx.DependsOnSourceAsset(BlurShaderPath);

            const TextureFormat textureFormat = TextureFormat.R8;
            var texture = new Texture2D(Width, Height, textureFormat, GenerateMipMaps, true)
            {
                wrapMode = TextureWrapMode.Clamp,
            };

            var depthRt = RenderTexture.GetTemporary(Width, Height, 32, RenderTextureFormat.Depth);
            var tempRt1 = RenderTexture.GetTemporary(Width, Height, 0, RenderTextureFormat.R8);
            var tempRt2 = RenderTexture.GetTemporary(Width, Height, 0, RenderTextureFormat.R8);

            Shader blurShader = AssetDatabase.LoadAssetAtPath<Shader>(BlurShaderPath);
            var blurMaterial = new Material(blurShader);

            using (var cmd = new CommandBuffer())
            {
                cmd.name = ctx.assetPath;
                cmd.SetRenderTarget(depthRt);
                cmd.ClearRenderTarget(true, false, Color.black);

                var viewMatrix = Matrix4x4.Inverse(
                    Matrix4x4.TRS(
                        new Vector3(-ModelPositionOffset.x, 0, -ModelPositionOffset.y),
                        Quaternion.LookRotation(Vector3.down, Vector3.forward),
                        Vector3.one
                    )
                );

                const float zRange = 1000.0f;
                var projectionMatrix = Matrix4x4.Ortho(
                    -BoundsSize * 0.5f, BoundsSize * 0.5f,
                    -BoundsSize * 0.5f, BoundsSize * 0.5f,
                    -zRange, zRange
                );

                cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

                Renderers.Clear();
                Model.GetComponentsInChildren(Renderers);

                foreach (Renderer renderer in Renderers)
                {
                    if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer && IncludeSkinnedRenderers)
                    {
                        DrawRenderer(cmd, renderer);
                    }
                }

                Renderers.Clear();

                cmd.SetRenderTarget(tempRt1);
                ToonBlitter.BlitDefault(cmd, depthRt, true);

                for (int i = 0; i < BlurIterations; i++)
                {
                    Blur(cmd, tempRt1, tempRt2, blurMaterial, i);
                }

                Graphics.ExecuteCommandBuffer(cmd);
            }


            ReadbackFromRenderTexture(tempRt1, texture);
            RenderTexture.ReleaseTemporary(depthRt);
            RenderTexture.ReleaseTemporary(tempRt1);
            RenderTexture.ReleaseTemporary(tempRt2);

            DestroyImmediate(blurMaterial);

            if (Compressed)
            {
                texture.Compress(HighQualityCompression);
            }

            ctx.AddObjectToAsset("texture", texture, texture);
            ctx.SetMainObject(texture);
        }

        private void ReadbackFromRenderTexture(RenderTexture sourceRt, Texture2D destination)
        {
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = sourceRt;
            destination.ReadPixels(new Rect(0, 0, Height, Height), 0, 0, false);
            destination.Apply(GenerateMipMaps, !Compressed && !ReadWriteEnabled);
            RenderTexture.active = previousActive;
        }

        private static void DrawRenderer(CommandBuffer cmd, Renderer renderer)
        {
            Mesh mesh;
            switch (renderer)
            {
                case MeshRenderer:
                {
                    MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                    if (meshFilter == null)
                    {
                        return;
                    }

                    mesh = meshFilter.sharedMesh;
                    break;
                }
                case SkinnedMeshRenderer skinnedMeshRenderer:
                {
                    mesh = skinnedMeshRenderer.sharedMesh;
                    break;
                }
                default:
                    return;
            }


            Materials.Clear();
            renderer.GetSharedMaterials(Materials);
            int subMeshCount = Mathf.Min(Materials.Count, mesh.subMeshCount);

            for (int index = 0; index < subMeshCount; index++)
            {
                Material material = Materials[index];
                cmd.DrawRenderer(renderer, material, index, 2);
            }

            Materials.Clear();
        }

        private static void Blur(CommandBuffer cmd, RenderTexture rt1, RenderTexture rt2, Material blurMaterial, int i)
        {
            const bool renderToTexture = true;

            // Horizontal
            cmd.SetRenderTarget(rt2);
            cmd.SetGlobalTexture(ToonBlitter.MainTexId, rt1);
            cmd.SetGlobalInt(ApplyStepToSourceSamplesId, i == 0 ? 1 : 0);
            cmd.SetGlobalVector(DirectionId, new Vector2(1, 0));
            ToonBlitter.Blit(cmd, blurMaterial, renderToTexture, 0);

            // Vertical
            cmd.SetRenderTarget(rt1);
            cmd.SetGlobalTexture(ToonBlitter.MainTexId, rt2);
            cmd.SetGlobalInt(ApplyStepToSourceSamplesId, 0);
            cmd.SetGlobalVector(DirectionId, new Vector2(0, 1));
            ToonBlitter.Blit(cmd, blurMaterial, renderToTexture, 0);
        }
    }
}