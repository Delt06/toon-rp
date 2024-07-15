using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [VolumeComponentMenu("ToonRP/Toon Bloom")]
    public class ToonBloomComponent : VolumeComponent
    {
        [Header("General")]
        public MinFloatParameter Intensity = new MinFloatParameter(1f, 0f);
        public MinFloatParameter Threshold = new MinFloatParameter(0.95f, 0f);
        public ClampedFloatParameter TresholdKnee = new ClampedFloatParameter(0.5f, 0f, 1f);

        [Header("Quality")]
        public ClampedIntParameter MaxIterations = new ClampedIntParameter(16, 0, ToonBloom.MaxIterations);
        public MinIntParameter ResolutionFactor = new MinIntParameter(2, 1);
        public MinIntParameter DownsampleLimit = new MinIntParameter(2, 1);

        [Header("Pattern")]
        public BoolParameter PatternEnabled = new BoolParameter(false);

        public MinFloatParameter PatternScale = new MinFloatParameter(350f, 0f);
        public MinFloatParameter PatternPower = new MinFloatParameter(2f, 0.001f);
        public MinFloatParameter PatternMultiplier = new MinFloatParameter(4f, 0f);

        public ClampedFloatParameter PatternSmoothness = new ClampedFloatParameter(0.5f, 0.001f, 1f);
        public ClampedFloatParameter PatternLuminanceTreshold = new ClampedFloatParameter(0.1f, 0.05f, 1f);
        public ClampedFloatParameter PatternLuminanceMultiplier = new ClampedFloatParameter(1f, 0f, 1f);
        public MinFloatParameter PatternDotSizeLimit = new MinFloatParameter(1, 0f);
        public ClampedFloatParameter PatternBlend = new ClampedFloatParameter(0.1f, 0f, 1f);
        public ClampedFloatParameter PatternFinalIntensityThreshold = new ClampedFloatParameter(0.25f, 0f, 0.99f);

        public bool IsActive() => Intensity.value > 0f;

        protected override void OnEnable()
        {
            base.OnEnable();
            displayName = "Toon Bloom";
        }
    }
}
