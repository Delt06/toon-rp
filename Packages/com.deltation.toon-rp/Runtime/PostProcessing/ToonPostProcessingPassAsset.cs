using System.Linq;
using UnityEngine;

namespace DELTation.ToonRP.PostProcessing
{
    public abstract class ToonPostProcessingPassAsset : ScriptableObject
    {
        public const string Path = "Toon RP/Post-Processing/";
        [SerializeField] [HideInInspector] private Shader[] _forceIncludedShaders;

        protected virtual void OnValidate()
        {
            if (_forceIncludedShaders == null || _forceIncludedShaders.Length != ForceIncludedShaderNames().Length)
            {
                _forceIncludedShaders = ForceIncludedShaderNames().Select(Shader.Find).ToArray();
            }
        }

        public virtual int Order() => 0;

        public abstract IToonPostProcessingPass CreatePass();

        protected abstract string[] ForceIncludedShaderNames();
    }

    public abstract class ToonPostProcessingPassAsset<TSettings> : ToonPostProcessingPassAsset
    {
        [ToonRpHeader("Settings")]
        public TSettings Settings;
    }
}