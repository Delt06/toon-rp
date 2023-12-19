using System;
using UnityEngine;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    [Serializable]
    public struct ToonFakeAdditionalLightsSettings
    {
        public TextureSize Size;
        [Min(0.01f)]
        public float MaxDistance;
        public float ReceiverPlaneY;
    }
}