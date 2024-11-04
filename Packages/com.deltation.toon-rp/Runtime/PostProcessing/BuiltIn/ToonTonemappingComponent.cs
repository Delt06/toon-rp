using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [VolumeComponentMenu("ToonRP/Toon Tone Mapping")]
    public class ToonToneMappingComponent : VolumeComponent
    {
        [Header("General")]
        public BoolParameter Enabled = new BoolParameter(false);
        public MinFloatParameter Exposure = new MinFloatParameter(0f, 0.1f);


        public bool IsActive() => Enabled.value;

        protected override void OnEnable()
        {
            base.OnEnable();
            displayName = "Toon Tone Mapping";
        }
    }
}
