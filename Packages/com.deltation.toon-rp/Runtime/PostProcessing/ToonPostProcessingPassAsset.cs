using System.Linq;
using DELTation.ToonRP.Attributes;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing
{
    public abstract class ToonPostProcessingPassAsset : ScriptableObject
    {
        public const string Path = "Toon RP/Post-Processing/";
        const bool overrideSettings = true;

        [SerializeField][HideInInspector] private Shader[] _forceIncludedShaders;

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

        /// <summary>
        /// Copy this pass's default settings to the given volume profile.
        /// </summary>
        /// <param name="profile"></param>
        public virtual void CopySettingsToVolumeProfile(VolumeProfile profile)
        {
            return;
        }

        /// <summary>
        /// Gets or adds a volume component from the VolumeProfile.
        /// </summary>
        /// <typeparam name="T">VolumeComponent type</typeparam>
        /// <param name="profile">Profile to check for Component</param>
        /// <returns></returns>
        public static T GetOrAddVolumeComponent<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet(out T component))
                return component;

            return profile.Add<T>(overrideSettings);
        }
    }

    public abstract class ToonPostProcessingPassAsset<TSettings> : ToonPostProcessingPassAsset
    {
        [ToonRpHeader("Settings", Size = ToonRpHeaderAttribute.DefaultSize + 2)]
        public TSettings Settings;
    }
}