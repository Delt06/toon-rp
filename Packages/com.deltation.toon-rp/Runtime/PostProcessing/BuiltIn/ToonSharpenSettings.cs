using System;
using UnityEngine;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [Serializable]
    public struct ToonSharpenSettings
    {
        [Range(0.0f, 1.0f)]
        public float Amount;
    }
}