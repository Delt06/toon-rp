using System;
using DELTation.ToonRP.Editor.ShaderGraph.Targets;
using UnityEngine;

namespace DELTation.ToonRP.Editor.ShaderGraph
{
    // This is a metadata object attached to ShaderGraph import asset results by the Universal Target
    // it contains any additional information that we might want to know about the Universal shader
    [Serializable]
    internal sealed class ToonMetadata : ScriptableObject
    {
        [SerializeField] private bool _allowMaterialOverride;
        [SerializeField] private AlphaMode _alphaMode;
        [SerializeField] private bool _castShadows;
        [SerializeField] private ToonShaderUtils.ShaderID _shaderID;

        public bool AllowMaterialOverride
        {
            get => _allowMaterialOverride;
            set => _allowMaterialOverride = value;
        }

        public AlphaMode AlphaMode
        {
            get => _alphaMode;
            set => _alphaMode = value;
        }

        public bool CastShadows
        {
            get => _castShadows;
            set => _castShadows = value;
        }

        public ToonShaderUtils.ShaderID ShaderID
        {
            get => _shaderID;
            set => _shaderID = value;
        }
    }
}