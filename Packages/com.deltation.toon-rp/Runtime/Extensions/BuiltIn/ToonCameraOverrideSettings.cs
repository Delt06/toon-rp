using System;
using DELTation.ToonRP.Attributes;
using UnityEngine;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    [Serializable]
    public struct ToonCameraOverrideSettings
    {
        public bool Enabled;
        [ToonRpShowIf(nameof(Enabled))] [Range(4.0f, 179.0f)]
        public float FieldOfView;

        public static ToonCameraOverrideSettings Default => new()
        {
            Enabled = false,
            FieldOfView = 60f,
        };
    }
}