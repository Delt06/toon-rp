#ifndef TOON_RP_FOG
#define TOON_RP_FOG

#include "Common.hlsl"

#if !defined(_FORCE_DISABLE_FOG) && (defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2))
#define FOG_ANY
#endif // FOG_LINEAR || FOG_EXP || FOG_EXP2

real ComputeFogFactorZ0ToFar(float z)
{
    #if defined(FOG_LINEAR)
    // factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
    float fogFactor = saturate(z * unity_FogParams.z + unity_FogParams.w);
    return real(fogFactor);
    #elif defined(FOG_EXP) || defined(FOG_EXP2)
    // factor = exp(-(density*z)^2)
    // -density * z computed at vertex
    return real(unity_FogParams.x * z);
    #else
    return real(0.0);
    #endif
}

real ComputeFogFactor(float zPositionCS)
{
    float clipZ_0Far = UNITY_Z_0_FAR_FROM_CLIPSPACE(zPositionCS);
    return ComputeFogFactorZ0ToFar(clipZ_0Far);
}

float ComputeFogIntensity(float fogFactor)
{
    float fogIntensity = 0.0;
    #ifdef FOG_ANY
    #if defined(FOG_EXP)
    // factor = exp(-density*z)
    // fogFactor = density*z compute at vertex
    fogIntensity = saturate(exp2(-fogFactor));
    #elif defined(FOG_EXP2)
    // factor = exp(-(density*z)^2)
    // fogFactor = density*z compute at vertex
    fogIntensity = saturate(exp2(-fogFactor * fogFactor));
    #elif defined(FOG_LINEAR)
    fogIntensity = fogFactor;
    #endif
    #endif // FOG_ANY
    return fogIntensity;
}

float3 MixFogColor(float3 fragColor, float3 fogColor, float fogFactor)
{
    #ifdef FOG_ANY
    float fogIntensity = ComputeFogIntensity(fogFactor);
    fragColor = lerp(fogColor, fragColor, fogIntensity);
    #endif // FOG_ANY
    return fragColor;
}

float3 MixFog(float3 fragColor, float fogFactor)
{
    return MixFogColor(fragColor, unity_FogColor.rgb, fogFactor);
}

#ifdef FOG_ANY

#define TOON_RP_FOG_FACTOR_INTERPOLANT float fogFactor : FOG_FACTOR;
#define TOON_RP_FOG_FACTOR_TRANSFER(OUT, positionCs) OUT.fogFactor = ComputeFogFactor(positionCs.z);
#define TOON_RP_FOG_MIX(IN, color) color = MixFog(color, IN.fogFactor);

#else // !FOG_ANY

#define TOON_RP_FOG_FACTOR_INTERPOLANT
#define TOON_RP_FOG_FACTOR_TRANSFER(OUT, positionCs)
#define TOON_RP_FOG_MIX(IN, color)

#endif // FOG_ANY

#endif // TOON_RP_FOG