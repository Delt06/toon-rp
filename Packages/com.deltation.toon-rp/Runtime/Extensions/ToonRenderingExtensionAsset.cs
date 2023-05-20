using System.Linq;
using DELTation.ToonRP.Attributes;
using UnityEngine;

namespace DELTation.ToonRP.Extensions
{
    public abstract class ToonRenderingExtensionAsset : ScriptableObject
    {
        public const string Path = "Toon RP/Extensions/";

        [SerializeField] [HideInInspector] private Shader[] _forceIncludedShaders;

        public abstract ToonRenderingEvent Event { get; }

        protected virtual void OnValidate()
        {
            if (_forceIncludedShaders == null || _forceIncludedShaders.Length != ForceIncludedShaderNames().Length)
            {
                _forceIncludedShaders = ForceIncludedShaderNames().Select(Shader.Find).ToArray();
            }
        }

        public virtual bool RequiresStencil() => false;

        public virtual ToonCameraRendererSettings.DepthPrePassMode RequiredDepthPrePassMode() =>
            ToonCameraRendererSettings.DepthPrePassMode.Off;

        public abstract IToonRenderingExtension CreateExtension();

        protected abstract string[] ForceIncludedShaderNames();
    }

    public abstract class ToonRenderingExtensionAsset<TSettings> : ToonRenderingExtensionAsset
    {
        [ToonRpHeader("Settings", Size = ToonRpHeaderAttribute.DefaultSize + 2)]
        public TSettings Settings;
    }
}