using System;
using UnityEngine;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [Serializable]
    public struct ToonSharpenSettings
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