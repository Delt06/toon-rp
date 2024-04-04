﻿#ifndef TOON_RP_DEFAULT_META_PASS
#define TOON_RP_DEFAULT_META_PASS

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/MetaPass.hlsl"
#include "../ShaderLibrary/NormalMap.hlsl"
#include "../ShaderLibrary/Textures.hlsl"

v2f VS(const appdata IN)
{
    return ToonVertexMeta(IN);
}

half4 PS(const v2f IN) : SV_Target
{
    const float4 albedo = SampleAlbedo(IN.uv);

    #ifdef _ALPHATEST_ON
    AlphaClip(albodo.a);
    #endif // _ALPHATEST_ON

    #ifdef EMISSION
    const float3 emission = _EmissionColor * albedo.a;
    #else // !EMISSION
    const float3 emission = 0;
    #endif // EMISSION

    MetaInput metaInput;
    metaInput.Albedo = albedo.rgb;
    metaInput.Emission = emission;
    return ToonFragmentMeta(IN, metaInput);
}

#endif // TOON_RP_DEFAULT_META_PASS