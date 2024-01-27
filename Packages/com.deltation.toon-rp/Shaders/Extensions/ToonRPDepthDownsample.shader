Shader "Hidden/Toon RP/Depth Downsample"
{
    Properties 
    {
    }
	SubShader
	{
	    Pass
		{
		    Name "Toon RP Depth Downsample"
		    
		    ZWrite On
		    ZTest Off
		    Cull Off
		    ColorMask 0
			
			HLSLPROGRAM

			#pragma multi_compile_local_fragment _ _HIGH_QUALITY

			//#pragma enable_d3d11_debug_symbols

			#include "../../ShaderLibrary/Common.hlsl"
		    #include "../../ShaderLibrary/Textures.hlsl"

		    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			#include "../../ShaderLibrary/DepthNormals.hlsl"

		    #pragma vertex Vert
		    #pragma fragment Frag

			uint _ResolutionFactor;

			#ifdef UNITY_REVERSED_Z

			#define ACCUMULATE_DEPTH min
			#define INITIAL_ACCUMULATED_DEPTH 1

			#else // !UNITY_REVERSED_Z

			#define ACCUMULATE_DEPTH max
			#define INITIAL_ACCUMULATED_DEPTH 0 
			
			#endif // UNITY_REVERSED_Z

			float SampleDepthTextureHighQuality(const float2 uv)
			{
			    const float2 texelSize = _ToonRP_DepthTexture_TexelSize.xy;
			    const uint halfFactor = _ResolutionFactor / 2;
			    const float evenOffset = _ResolutionFactor % 2 == 0 ? 0.5 : 0;
			    float accumulatedDepth = INITIAL_ACCUMULATED_DEPTH;

                for (uint xi = 0; xi < _ResolutionFactor; ++xi)
			    {
			        for (uint yi = 0; yi < _ResolutionFactor; ++yi)
			        {
			            float2 uvOffset = float2(xi, yi);
			            uvOffset = uvOffset - halfFactor + evenOffset;
			            const float2 sampleUv = uv + texelSize * uvOffset;
			            const float depth = SampleDepthTexture(sampleUv);
			            accumulatedDepth = ACCUMULATE_DEPTH(depth, accumulatedDepth);
			        }
			    }
			    
			    return accumulatedDepth;
			}

            float4 Frag(const Varyings IN, out float outDepth : SV_Depth) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                const float2 uv = UnityStereoTransformScreenSpaceTex(IN.texcoord);
                #ifdef _HIGH_QUALITY
                outDepth = SampleDepthTextureHighQuality(uv);
                #else // !_HIGH_QUALITY
                outDepth = SampleDepthTexture(uv);
                #endif // _HIGH_QUALITY
                return 0;
            }
			
			ENDHLSL
		}
	}
}