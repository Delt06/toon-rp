Shader "Hidden/Toon RP/Blit"
{
	Properties
	{
	}
	SubShader
	{
		Pass
		{
		    Name "Toon RP Blit"
		    
		    ZTest Off
	        ZWrite Off
	        Cull Off
			
			HLSLPROGRAM

			//#pragma enable_d3d11_debug_symbols

	        #pragma vertex Vert
		    #pragma fragment Frag

			#include "Packages/com.deltation.toon-rp/ShaderLibrary/Common.hlsl"
			#include "Packages/com.deltation.toon-rp/ShaderLibrary/Textures.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_MainTex);
            SAMPLER(sampler_MainTex);

			float4 Frag(const Varyings IN) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                const float2 uv = UnityStereoTransformScreenSpaceTex(IN.texcoord);
                return SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, uv);
            }

			ENDHLSL
		}
	}
}