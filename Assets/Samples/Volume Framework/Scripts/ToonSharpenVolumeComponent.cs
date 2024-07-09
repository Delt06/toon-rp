using UnityEngine;
using UnityEngine.Rendering;
using static DELTation.ToonRP.PostProcessing.BuiltIn.ToonSharpenSettingsTest;

[VolumeComponentMenu("ToonRP/Toon Sharpen")]
public class ToonSharpenVolumeComponent : VolumeComponent
{
    public ClampedFloatParameter amount = new ClampedFloatParameter(0f, 0f, 10f);
    public EnumParameter<PassOrder> Order = new EnumParameter<PassOrder>(PassOrder.PreUpscale);
    public bool IsActive() => amount.value > 0f;
    
    protected override void OnEnable()
    {
        base.OnEnable();
        displayName = "Toon Sharpen";
    }

}
