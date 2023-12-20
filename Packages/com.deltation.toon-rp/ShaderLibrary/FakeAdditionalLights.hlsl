#ifndef TOON_RP_FAKE_ADDITIONAL_LIGHTS
#define TOON_RP_FAKE_ADDITIONAL_LIGHTS

#include "Math.hlsl"

CBUFFER_START(_ToonRP_FakeAdditionalLights)
    half4 _ToonRP_FakeAdditionalLights_Bounds_MultiplierOffset;
    half _ToonRP_FakeAdditionalLights_ReceiverPlaneY;
    float2 _ToonRP_FakeAdditionalLights_Ramp;
    float4 _ToonRP_FakeAdditionalLights_Fades;
CBUFFER_END

TEXTURE2D(_FakeAdditionalLightsTexture);
SAMPLER(sampler_FakeAdditionalLightsTexture);

half2 FakeAdditionalLights_PositionToUV(const half2 positionWsXz)
{
    const half2 multiplier = _ToonRP_FakeAdditionalLights_Bounds_MultiplierOffset.xy;
    const half2 offset = _ToonRP_FakeAdditionalLights_Bounds_MultiplierOffset.zw;
    return positionWsXz * multiplier + offset;
}

float4 FakeAdditionalLights_SampleRaw(const float3 positionWs)
{
    const half2 uv = FakeAdditionalLights_PositionToUV(positionWs.xz);
    const float4 sample = SAMPLE_TEXTURE2D(_FakeAdditionalLightsTexture, sampler_FakeAdditionalLightsTexture, uv);
    return sample;
}

float FakeAdditionalLights_DistanceFade(const float3 positionWs)
{
    const float distanceSq = DistanceSquared(positionWs, _WorldSpaceCameraPos);
    return DistanceFadeBase(distanceSq, _ToonRP_FakeAdditionalLights_Fades.x, _ToonRP_FakeAdditionalLights_Fades.y);
}

float FakeAdditionalLights_HeightFade(const float yWs)
{
    return DistanceFadeBase(yWs - _ToonRP_FakeAdditionalLights_ReceiverPlaneY,
        _ToonRP_FakeAdditionalLights_Fades.z, _ToonRP_FakeAdditionalLights_Fades.w);
}

#endif // TOON_RP_FAKE_ADDITIONAL_LIGHTS