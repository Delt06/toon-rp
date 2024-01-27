#ifndef TOON_RP_INSTANCING
#define TOON_RP_INSTANCING

// This is a workaround for shader compiler bug:
// Sometimes, in multiview mode, the interpolated eye index would contain garbage data.
// Adding nointerpolation to the eye index interpolator fixes the issue.

#ifdef UNITY_STEREO_INSTANCING_ENABLED
#if defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)
    #define TOON_DEFAULT_UNITY_VERTEX_OUTPUT_STEREO                          nointerpolation uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex; nointerpolation uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#elif defined(SHADER_API_PSSL) && defined(TESSELLATION_ON)
    #if defined(SHADER_STAGE_VERTEX)
        #define TOON_DEFAULT_UNITY_VERTEX_OUTPUT_STEREO                          nointerpolation uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
    #else
        #define TOON_DEFAULT_UNITY_VERTEX_OUTPUT_STEREO                          nointerpolation uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex; nointerpolation uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
    #endif
#else
    #define TOON_DEFAULT_UNITY_VERTEX_OUTPUT_STEREO                          nointerpolation uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif

#elif defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    #define TOON_DEFAULT_UNITY_VERTEX_OUTPUT_STEREO nointerpolation float stereoTargetEyeIndexAsBlendIdx0 : BLENDWEIGHT0;
#else
    #define TOON_DEFAULT_UNITY_VERTEX_OUTPUT_STEREO
#endif


#if !defined(UNITY_VERTEX_OUTPUT_STEREO)
#   define UNITY_VERTEX_OUTPUT_STEREO                           TOON_DEFAULT_UNITY_VERTEX_OUTPUT_STEREO
#endif

#endif // TOON_RP_INSTANCING