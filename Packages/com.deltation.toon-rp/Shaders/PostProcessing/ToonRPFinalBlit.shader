Shader "Hidden/Toon RP/FinalBlit"
{
    SubShader
	{
	    Pass
		{
		    Name "Toon RP Final Blit"
		    
		    Cull Off ZWrite Off ZTest Always

			HLSLPROGRAM

			//#pragma enable_d3d11_debug_symbols

	        #pragma vertex Vert
		    #pragma fragment Frag
			
			#include "Packages/com.deltation.toon-rp/ShaderLibrary/Common.hlsl"
			#include "Packages/com.deltation.toon-rp/ShaderLibrary/Textures.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			TEXTURE2D_X(_BlitSource);
			SAMPLER(sampler_BlitSource);

			float4 Frag(const Varyings IN) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                
                const float2 uv = UnityStereoTransformScreenSpaceTex(IN.texcoord);
                float4 color = SAMPLE_TEXTURE2D_X(_BlitSource, sampler_BlitSource, uv);
                return color;
            }

			ENDHLSL
		}
	}
}