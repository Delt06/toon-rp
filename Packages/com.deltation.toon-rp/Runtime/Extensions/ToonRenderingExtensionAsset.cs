using System.Linq;
using DELTation.ToonRP.Attributes;
using JetBrains.Annotations;
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

        public abstract bool UsesRenderingEvent(ToonRenderingEvent renderingEvent);

        public virtual bool RequiresStencil() => false;

        public virtual ToonPrePassRequirement RequiredPrePassMode() => ToonPrePassRequirement.Off;

        [CanBeNull]
        public abstract IToonRenderingExtension CreateExtensionOrDefault(ToonRenderingEvent renderingEvent);

        protected abstract string[] ForceIncludedShaderNames();
    }

    public abstract class ToonRenderingExtensionAsset<TSettings> : ToonRenderingExtensionAsset
    {
        [ToonRpHeader("Settings", Size = ToonRpHeaderAttribute.DefaultSize + 2)]
        public TSettings Settings;
    }
}