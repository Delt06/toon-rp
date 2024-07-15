using DELTation.ToonRP.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [VolumeComponentMenu("ToonRP/Toon Film Grain")]
    public class ToonFilmGrainComponent : VolumeComponent
    {
        [Header("General")]
        public BoolParameter Enabled = new BoolParameter(false);
        public ClampedFloatParameter Intensity = new ClampedFloatParameter(0.5f, 0f, 1f);        
        public MinFloatParameter LuminanceThreshold = new MinFloatParameter(0.5f, 0f);
        public Texture2DParameter Texture = new Texture2DParameter(null);


        public bool IsActive() => Enabled.value && Intensity.value > 0;

        protected override void OnEnable()
        {
            base.OnEnable();
            displayName = "Toon Film Grain";
        }
    }
}
