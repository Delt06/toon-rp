using System;
using UnityEngine;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [Serializable]
    public struct ToonSharpenSettingsTest
    {
        public enum PassOrder
        {
            PreUpscale,
            PostUpscale,
        }

        public PassOrder Order;

        [Range(-0.0f, 10.0f)]
        public float Amount;
    }
}