using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [VolumeComponentMenu("ToonRP/Toon Lookup Table (LUT)")]
    public class ToonLookupTableComponent : VolumeComponent
    {
        [Header("General")]
        //TODO: Convert enabled to intensity controls?
        public BoolParameter Enabled = new BoolParameter(false);
        public Texture2DParameter Texture = new Texture2DParameter(null);

        public bool IsActive() => Enabled.value && Texture.value != null;

        protected override void OnEnable()
        {
            base.OnEnable();
            displayName = "Toon Lookup Table";
        }
    }
}
