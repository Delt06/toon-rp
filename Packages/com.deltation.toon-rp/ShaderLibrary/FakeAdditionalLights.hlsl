#ifndef TOON_RP_FAKE_ADDITIONAL_LIGHTS
#define TOON_RP_FAKE_ADDITIONAL_LIGHTS

half4 _ToonRP_FakeAdditionalLights_Bounds_MultiplierOffset;

TEXTURE2D(_FakeAdditionalLightsTexture);
SAMPLER(sampler_FakeAdditionalLightsTexture);

half2 FakeAdditionalLights_PositionToUV(const half2 positionWsXz)
{
    const half2 multiplier = _ToonRP_FakeAdditionalLights_Bounds_MultiplierOffset.xy;
    const half2 offset = _ToonRP_FakeAdditionalLights_Bounds_MultiplierOffset.zw;
    return positionWsXz * multiplier + offset;
}

float3 FakeAdditionalLights_Sample(const float3 positionWs)
{
    const half2 uv = FakeAdditionalLights_PositionToUV(positionWs.xz);
    return SAMPLE_TEXTURE2D(_FakeAdditionalLightsTexture, sampler_FakeAdditionalLightsTexture, uv).rgb;
}

#endif // TOON_RP_FAKE_ADDITIONAL_LIGHTS