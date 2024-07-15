using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [VolumeComponentMenu("ToonRP/Toon Basic Fog")]
    public class ToonFogComponent : VolumeComponent
    {
        [Header("General")]
        public ColorParameter FogColor = new ColorParameter(Color.gray);

        public bool IsActive() => FogColor.value.a > 0;

        protected override void OnEnable()
        {
            base.OnEnable();
            displayName = "Toon Basic Fog";
        }

        public void LoadValuesFromRenderSettings()
        {
            FogColor.value = RenderSettings.fogColor;
        }
    }
}
