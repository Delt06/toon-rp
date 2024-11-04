using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [VolumeComponentMenu("ToonRP/Toon Motion Blur")]
    public class ToonMotionBlurComponent : VolumeComponent
    {
        [Header("General")]
        public MinFloatParameter Strength = new MinFloatParameter(1.0f, 0.0f);
        
        public MinIntParameter NumSamples = new MinIntParameter(8, 2);

        public bool IsActive() => Strength.value > 0;

        protected override void OnEnable()
        {
            base.OnEnable();
            displayName = "Toon Motion Blur";
        }
    }
}
