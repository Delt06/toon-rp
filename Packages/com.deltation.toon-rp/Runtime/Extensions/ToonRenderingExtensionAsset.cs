using System.Linq;
using UnityEngine;

namespace DELTation.ToonRP.Extensions
{
    public abstract class ToonRenderingExtensionAsset : ScriptableObject
    {
        public const string Path = "Toon RP/Extensions/";

        [SerializeField] [HideInInspector] private Shader[] _forceIncludedShaders;

        protected virtual void OnValidate()
        {
            if (_forceIncludedShaders == null || _forceIncludedShaders.Length != ForceIncludedShaderNames().Length)
            {
                _forceIncludedShaders = ForceIncludedShaderNames().Select(Shader.Find).ToArray();
            }
        }
        
        public abstract ToonRenderingEvent Event { get; }

        public virtual ToonCameraRendererSettings.DepthPrePassMode RequiredDepthPrePassMode() =>
            ToonCameraRendererSettings.DepthPrePassMode.Off;

        public abstract IToonRenderingExtension CreateExtension();

        protected abstract string[] ForceIncludedShaderNames();
    }
}