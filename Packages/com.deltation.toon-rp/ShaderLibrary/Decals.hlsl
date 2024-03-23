#ifndef TOON_RP_DECALS
#define TOON_RP_DECALS

#include "Packages/com.deltation.toon-rp/ShaderLibrary/DepthNormals.hlsl"

float2 ComputeDecalSpaceUv(const float2 screenUv, out half3 clipValue)
{
    const float ndcDepth = RawToNdcDepth(SampleDepthTexture(screenUv));

    const float3 reconstructedPositionWs = ComputeWorldSpacePosition(screenUv, ndcDepth, UNITY_MATRIX_I_VP);
    const half3 reconstructedPositionOs = TransformWorldToObject(reconstructedPositionWs);

    clipValue = 0.5h - reconstructedPositionOs;

    return reconstructedPositionOs.xy + 0.5h; // [-0.5, 0.5] -> [0.0, 1.0]
}

half ComputeDecalAngleClipValue(const float2 screenUv, const half angleThreshold)
{
    const float3 sceneNormalsWs = SampleNormalsTexture(screenUv);
    const float3 sceneNormalOs = TransformWorldToObjectNormal(sceneNormalsWs);
    const half clipAngle = step(angleThreshold, (half) sceneNormalOs.z);
    return -clipAngle;
}

void DecalClip(const half3 clipValue)
{
    clip(clipValue);
}

#endif // TOON_RP_DECALS