#ifndef TOON_RP_VSM
#define TOON_RP_VSM

static const float ToonRp_Vsm_DepthScale = 0.1f;

float PackVsmDepth(const float depth)
{
    return depth * ToonRp_Vsm_DepthScale;
}

#ifdef _TOON_RP_VSM
float GetPackedVsmDepth(const float3 positionWs)
{
    float viewZ = TransformWorldToView(positionWs).z;
    #ifdef UNITY_REVERSED_Z
    viewZ *= -1.0f;
    #endif // UNITY_REVERSED_Z
    return PackVsmDepth(viewZ);
}
#endif // _TOON_RP_VSM



#endif // TOON_RP_VSM