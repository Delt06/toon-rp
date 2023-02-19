#ifndef TOON_RP_SHADOWS
#define TOON_RP_SHADOWS

static const float ToonRp_Vsm_DepthScale = 0.1f;

float PackVsmDepth(const float depth)
{
    return depth * ToonRp_Vsm_DepthScale;
}

#define MAX_DIRECTIONAL_LIGHT_COUNT 1

TEXTURE2D(_ToonRP_DirectionalShadowAtlas);
SAMPLER(sampler_ToonRP_DirectionalShadowAtlas);

CBUFFER_START(_ToonRpShadows)
float4x4 _ToonRP_DirectionalShadowMatrices_VP[MAX_DIRECTIONAL_LIGHT_COUNT];
float4x4 _ToonRP_DirectionalShadowMatrices_V[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

inline float SampleShadowAttenuation(const float3 shadowCoords)
{
    const float2 varianceSample = SAMPLE_TEXTURE2D(_ToonRP_DirectionalShadowAtlas,
                                                   sampler_ToonRP_DirectionalShadowAtlas,
                                                   shadowCoords.xy).rg;
    const float variance = varianceSample.y - varianceSample.x * varianceSample.x;
    const float difference = shadowCoords.z - varianceSample.x;
    if (difference > 0.00001)
    {
        return smoothstep(0.4, 1.0, variance / (variance + difference * difference));
    }

    return 1.0;
}

float3 TransformWorldToShadowCoords(const float3 positionWs, const bool perspectiveProjection = false)
{
    const uint lightIndex = 0;
    float4 shadowCoords = mul(_ToonRP_DirectionalShadowMatrices_VP[lightIndex], float4(positionWs, 1.0f));
    shadowCoords.z = mul(_ToonRP_DirectionalShadowMatrices_V[lightIndex], float4(positionWs, 1.0f)).z;

    if (perspectiveProjection)
    {
        shadowCoords.xyz /= shadowCoords.w;
    }

    shadowCoords.xy = shadowCoords.xy * 0.5f + 0.5f; // [-1; 1] -> [0, 1]

    shadowCoords.z = PackVsmDepth(shadowCoords.z);

    #ifdef UNITY_REVERSED_Z
    shadowCoords.z *= -1.0f;
    #endif // UNITY_REVERSED_Z

    return shadowCoords.xyz;
}

#endif // TOON_RP_SHADOWS