using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing
{
    public static class ToonPostProcessingExtensions
    {
        public static bool InterruptsGeometryRenderPass(
            this ToonPostProcessing postProcessing
        ) =>
            postProcessing.TrueForAny(
                (IToonPostProcessingPass pass, in ToonPostProcessingContext context) =>
                    pass.InterruptsGeometryRenderPass(context)
            );

        /// <summary>
        /// Gets or adds a volume component from the VolumeProfile.
        /// </summary>
        /// <typeparam name="T">VolumeComponent type</typeparam>
        /// <param name="profile">Profile to check for Component</param>
        /// <returns></returns>
        public static T GetOrAddVolumeComponent<T>(this VolumeProfile profile) where T : VolumeComponent
        {
            const bool overrideSettings = true;

            if (profile.TryGet(out T component))
                return component;

            return profile.Add<T>(overrideSettings);
        }
    }
}