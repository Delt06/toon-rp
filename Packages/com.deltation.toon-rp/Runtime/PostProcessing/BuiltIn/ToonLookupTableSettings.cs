using System;
using DELTation.ToonRP.Attributes;
using UnityEngine;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [Serializable]
    public struct ToonLookupTableSettings
    {
        public bool Enabled;
        [ToonRpShowIf(nameof(Enabled))]
        public Texture2D Texture;
    }
}