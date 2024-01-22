Shader "Hidden/Toon RP/CopyDepth"
{
    SubShader
    {
        Tags { "RenderPipeline" = "ToonRP" }

        Pass
        {
            Name "CopyDepth"
            ZTest Always ZWrite On ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #pragma multi_compile_local_fragment _ _DEPTH_MSAA_2 _DEPTH_MSAA_4 _DEPTH_MSAA_8

            #define _OUTPUT_DEPTH
            #include "Packages/com.deltation.toon-rp/Shaders/Utils/CopyDepthPass.hlsl"

            ENDHLSL
        }
    }
}
