using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using static DELTation.ToonRP.PostProcessing.BuiltIn.ToonScreenSpaceOutlineSettings;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [VolumeComponentMenu("ToonRP/Toon Screen Space Outlines")]
    public class ToonScreenSpaceOutlineComponent : VolumeComponent
    {
        [Header("General")]
        public ColorParameter OutlineColor = new ColorParameter(Color.black);
        public BoolParameter UseFog = new BoolParameter(true);
        public MinFloatParameter MaxDistance = new MinFloatParameter(100f, 0f);
        public ClampedFloatParameter DistanceFade = new ClampedFloatParameter(0.1f, 0.001f, 1f);


        [Header("Filters")]
        public OutlineFilterParameter ColorFilter = new OutlineFilterParameter(new OutlineFilterReduced());
        public OutlineFilterParameter DepthFilter = new OutlineFilterParameter(new OutlineFilterReduced());
        public OutlineFilterParameter NormalsFilter = new OutlineFilterParameter(new OutlineFilterReduced());


        public bool IsActive() => OutlineColor.value.a > 0f;

        protected override void OnEnable()
        {
            base.OnEnable();
            displayName = "Toon Screen Space Outlines";
        }
    }

    /// <summary>
    /// Simple container for outline filter settings
    /// </summary>
    [System.Serializable]
    public class OutlineFilterParameter : VolumeParameter<OutlineFilterReduced>
    {
        public OutlineFilterParameter(OutlineFilterReduced value, bool overrideState = false)
            : base(value, overrideState)
        {
        }

        public override void Interp(OutlineFilterReduced from, OutlineFilterReduced to, float t)
        {
            m_Value.Threshold = Mathf.Lerp(from.Threshold, to.Threshold, t);
            m_Value.Smoothness = Mathf.Lerp(from.Smoothness, to.Smoothness, t);
        }
    }
    [Serializable]
    public class OutlineFilterReduced
    {

        [Min(0.05f)]
        public float Threshold;
        [Min(0.0f)]
        public float Smoothness;

        // Cast to OutlineFilter
        public static implicit operator OutlineFilter(OutlineFilterReduced reduced) => new OutlineFilter
        {
            Threshold = reduced.Threshold,
            Smoothness = reduced.Smoothness
        };

        // Cast to OutlineFilterReduced
        public static implicit operator OutlineFilterReduced(OutlineFilter filter) => new OutlineFilterReduced
        {
            Threshold = filter.Threshold,
            Smoothness = filter.Smoothness
        };

    }
}
