using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [VolumeComponentMenu("ToonRP/Toon Sharpen")]
    public class ToonSharpenComponent : VolumeComponent
    {
        [Header("General")]
        public ClampedFloatParameter Amount = new ClampedFloatParameter(0f, -0.0f, 10f);
        public bool IsActive() => Amount.value > 0f;

        protected override void OnEnable()
        {
            base.OnEnable();
            displayName = "Toon Sharpen";
        }
    }
}
