using System;
using System.IO;
using System.Linq;
using DELTation.ToonRP.Attributes;
using DELTation.ToonRP.Shadows.Blobs;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace DELTation.ToonRP.Editor.BlobShadowBake
{
    [ScriptedImporter(1, Extension)]
    public class BakedBlobShadowsAtlasImporter : ScriptedImporter
    {
        public enum AtlasSize
        {
            _128 = 128,
            _256 = 256,
            _512 = 512,
            _1024 = 1024,
            _2048 = 2048,
            _4096 = 4096,
        }

        public const string Extension = "bakedblobshadowsatlas";

        public AtlasSize Size = AtlasSize._512;
        [Min(0)]
        public int Padding;

        public Texture2D[] SourceTextures;

        public bool GenerateMipMaps = true;
        public FilterMode FilterMode = FilterMode.Bilinear;
        [Range(0, 16)]
        public int AnisoLevel;

        [ToonRpHeader("Compression")]
        public bool Compressed = true;
        [ToonRpShowIf(nameof(Compressed))]
        public bool HighQualityCompression;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            SourceTextures ??= Array.Empty<Texture2D>();

            bool anyErrors = false;

            if (SourceTextures.Length > ToonBlobShadows.MaxBakedTextures)
            {
                ctx.LogImportError(
                    $"The number of textures ({SourceTextures.Length}) exceeds the limit ({ToonBlobShadows.MaxBakedTextures})."
                );
                anyErrors = true;
            }

            for (int index = 0; index < SourceTextures.Length; index++)
            {
                Texture2D sourceTexture = SourceTextures[index];
                if (sourceTexture == null)
                {
                    ctx.LogImportError($"Texture at {index} is null.");
                    anyErrors = true;
                    continue;
                }

                if (!sourceTexture.isReadable)
                {
                    ctx.LogImportError($"Texture at {index} is not readable. Ensure that it has Read/Write on.");
                    anyErrors = true;
                }
            }

            if (anyErrors)
            {
                return;
            }

            foreach (Texture2D sourceTexture in SourceTextures)
            {
                if (sourceTexture != null)
                {
                    continue;
                }

                string texturePath = AssetDatabase.GetAssetPath(sourceTexture);
                if (texturePath != null)
                {
                    ctx.DependsOnArtifact(texturePath);
                }
            }

            const TextureFormat textureFormat = TextureFormat.R8;
            var baseTexture = new Texture2D((int) Size, (int) Size, textureFormat, false, true);

            Rect[] rects = SourceTextures.Length > 0
                ? baseTexture.PackTextures(SourceTextures, Padding, (int) Size, false)
                : Array.Empty<Rect>();

            var texture = new Texture2D(baseTexture.width, baseTexture.height, textureFormat, GenerateMipMaps, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                name = Path.GetFileNameWithoutExtension(assetPath),
                anisoLevel = AnisoLevel,
                filterMode = FilterMode,
            };
            texture.SetPixels(baseTexture.GetPixels());
            bool makeNoLongerReadable = !Compressed;
            texture.Apply(true, makeNoLongerReadable);

            if (Compressed)
            {
                texture.Compress(HighQualityCompression);
            }

            ctx.AddObjectToAsset("texture", texture);

            ToonBlobShadowsAtlas atlas = ScriptableObject.CreateInstance<ToonBlobShadowsAtlas>();
            atlas.Texture = texture;
            atlas.TilingOffsets = rects.Select(r =>
                {
                    var textureSize = new Vector2(texture.width, texture.height);
                    Vector2 min = r.min;
                    Vector2 sizeNormalized = r.size;
                    return new Vector4(
                        sizeNormalized.x, sizeNormalized.y,
                        min.x, min.y
                    );
                }
            ).ToArray();
            ctx.AddObjectToAsset("atlas", atlas, texture);
            ctx.SetMainObject(atlas);
        }
    }
}