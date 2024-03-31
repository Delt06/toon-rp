// Adapted from https://github.com/Delt06/urp-toon-shader/blob/master/Packages/com.deltation.toon-shader/Assets/DELTation/ToonShader/Editor/NormalsSmoothing/NormalsSmoothingPostProcessor.cs

using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using static DELTation.ToonRP.Editor.NormalsSmoothing.NormalsSmoothingUtility;


namespace DELTation.ToonRP.Editor.NormalsSmoothing
{
    public class NormalsSmoothingPostProcessor : AssetPostprocessor
    {
        private const uint Version = 1;

        private const string Tag = "=SmoothedNormals=";
        private const string UvTag = "=UV=";
        private const string TangentsTag = "=Tangents=";

        [UsedImplicitly]
        private void OnPostprocessModel(GameObject gameObject)
        {
            string name = gameObject.name;

            if (name.Contains(Tag))
            {
                bool? toUv = null;

                bool hasUvTag = name.Contains(UvTag);
                bool hasTangentsTag = name.Contains(TangentsTag);

                if (hasUvTag && hasTangentsTag)
                {
                    Debug.LogWarning($"{name} has both {UvTag} and {TangentsTag}. Falling back to the default mode...");
                }
                else
                {
                    if (hasUvTag)
                    {
                        toUv = true;
                    }
                    else if (hasTangentsTag)
                    {
                        toUv = false;
                    }
                }

                Apply(gameObject, toUv);
            }
        }

        public override uint GetVersion() => Version;

        public override int GetPostprocessOrder() => 100;

        private float GetSmoothingAngle(GameObject gameObject)
        {
            Match match = Regex.Match(gameObject.name, @$"{Tag}(\d+)");
            if (!match.Success)
            {
                return MaxSmoothingAngle;
            }

            GroupCollection groupCollection = match.Groups;
            string groupValue = groupCollection[1].Value;
            if (float.TryParse(groupValue, NumberStyles.Any, CultureInfo.InvariantCulture, out float angle) &&
                angle is >= 0 and <= MaxSmoothingAngle)
            {
                return angle;
            }

            Debug.LogWarning($"{groupValue} is not a valid smoothing angle. Defaulting to {MaxSmoothingAngle}",
                gameObject
            );
            return MaxSmoothingAngle;
        }

        [UsedImplicitly]
        private void Apply(GameObject gameObject, bool? toUv)
        {
            var meshes = new HashSet<(Mesh, bool)>();
            float smoothingAngle = GetSmoothingAngle(gameObject);

            const bool includeInactive = true;
            foreach (MeshFilter meshFilter in gameObject.GetComponentsInChildren<MeshFilter>(includeInactive))
            {
                meshes.Add((meshFilter.sharedMesh, false));
            }

            foreach (SkinnedMeshRenderer skinnedMeshRenderer in
                     gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive))
            {
                meshes.Add((skinnedMeshRenderer.sharedMesh, true));
            }

            foreach ((Mesh mesh, bool isSkinned) in meshes)
            {
                int? uvChannel = toUv switch
                {
                    true => UvChannel,
                    false => null,
                    null => isSkinned ? null : UvChannel,
                };

                if (isSkinned && uvChannel.HasValue)
                {
                    Debug.LogError(
                        $"{gameObject.name}: exporting normals to UV even though it is a skinned mesh. The outlines won't look correctly. Skinned meshes should use tangents for custom normals."
                    );
                }

                mesh.CalculateNormalsAndWriteToChannel(smoothingAngle, uvChannel);
            }
        }
    }
}