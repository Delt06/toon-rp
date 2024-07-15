using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [VolumeComponentMenu("ToonRP/Toon Vignette")]
    public class ToonVignetteComponent : VolumeComponent
    {
        [Header("General")]
        public BoolParameter Enabled = new BoolParameter(false);
        public ClampedFloatParameter Intensity = new ClampedFloatParameter(0.5f, 0f, 1f);
        public ColorParameter VignetteColor = new ColorParameter(Color.black);

        [Header("Shape")]
        public ClampedFloatParameter CenterX = new ClampedFloatParameter(0.5f, 0f, 1f);
        public ClampedFloatParameter CenterY = new ClampedFloatParameter(0.5f, 0f, 1f);
        public ClampedFloatParameter Roundness = new ClampedFloatParameter(1f, 0f, 1f);
        public ClampedFloatParameter Smoothness = new ClampedFloatParameter(1f, 0f, 1f);


        public bool IsActive() => Enabled.value && Intensity.value > 0;

        protected override void OnEnable()
        {
            base.OnEnable();
            displayName = "Toon Vignette";
        }
    }
}
