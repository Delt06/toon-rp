using System.Linq;
using DELTation.ToonRP.Attributes;
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

        public virtual PrePassMode RequiredPrePassMode() => PrePassMode.Off;

        public abstract IToonPostProcessingPass CreatePass();

        protected abstract string[] ForceIncludedShaderNames();
    }

    public abstract class ToonPostProcessingPassAsset<TSettings> : ToonPostProcessingPassAsset
    {
        [ToonRpHeader("Settings", Size = ToonRpHeaderAttribute.DefaultSize + 2)]
        public TSettings Settings;
    }
}