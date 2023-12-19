#ifndef TOON_RP_FAKE_ADDITIONAL_LIGHTS
#define TOON_RP_FAKE_ADDITIONAL_LIGHTS

CBUFFER_START(_ToonRP_FakeAdditionalLights)
    half4 _Bounds_MultiplierOffset;
    half _ReceiverPlaneY;
    float2 _OverrideRamp;
CBUFFER_END

TEXTURE2D(_FakeAdditionalLightsTexture);
SAMPLER(sampler_FakeAdditionalLightsTexture);

half2 FakeAdditionalLights_PositionToUV(const half2 positionWsXz)
{
    const half2 multiplier = _Bounds_MultiplierOffset.xy;
    const half2 offset = _Bounds_MultiplierOffset.zw;
    return positionWsXz * multiplier + offset;
}

float4 FakeAdditionalLights_SampleRaw(const float3 positionWs)
{
    const half2 uv = FakeAdditionalLights_PositionToUV(positionWs.xz);
    const float4 sample = SAMPLE_TEXTURE2D(_FakeAdditionalLightsTexture, sampler_FakeAdditionalLightsTexture, uv);
    return sample;
}

#endif // TOON_RP_FAKE_ADDITIONAL_LIGHTS