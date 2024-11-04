using UnityEngine;

namespace DELTation.ToonRP
{
    /// <summary>
    /// Defines the update frequency for the Volume Framework.
    /// </summary>
    public enum VolumeFrameworkUpdateMode
    {
        /// <summary>
        /// Use this to have the Volume Framework update every frame.
        /// </summary>
        [InspectorName("Every Frame")]
        EveryFrame = 0,

        /// <summary>
        /// Use this to disable Volume Framework updates or to update it manually via scripting.
        /// </summary>
        [InspectorName("Via Scripting")]
        ViaScripting = 1,

        /// <summary>
        /// Use this to choose the setting set on the pipeline asset.
        /// </summary>
        [InspectorName("Use Pipeline Settings")]
        UsePipelineSettings = 2,
    }
}

