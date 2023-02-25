// Adapted from https://github.com/Delt06/urp-toon-shader/blob/master/Packages/com.deltation.toon-shader/Assets/DELTation/ToonShader/Editor/NormalsSmoothing/NormalsSmoothingPostProcessor.cs

using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using static ToonRP.Editor.NormalsSmoothing.NormalsSmoothingUtility;


namespace ToonRP.Editor.NormalsSmoothing
{
    public class NormalsSmoothingPostProcessor : AssetPostprocessor
    {
        private const string Tag = "SmoothedNormals";

        [UsedImplicitly]
        private void OnPostprocessModel(GameObject gameObject)
        {
            if (gameObject.name.Contains(Tag))
            {
                Apply(gameObject);
            }
        }

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
        private void Apply(GameObject gameObject)
        {
            var meshes = new HashSet<Mesh>();
            float smoothingAngle = GetSmoothingAngle(gameObject);

            const bool includeInactive = true;
            foreach (MeshFilter meshFilter in gameObject.GetComponentsInChildren<MeshFilter>(includeInactive))
            {
                meshes.Add(meshFilter.sharedMesh);
            }

            foreach (SkinnedMeshRenderer skinnedMeshRenderer in
                     gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive))
            {
                meshes.Add(skinnedMeshRenderer.sharedMesh);
            }

            foreach (Mesh mesh in meshes)
            {
                mesh.CalculateNormalsAndWriteToUv(smoothingAngle, UvChannel);
            }
        }
    }
}