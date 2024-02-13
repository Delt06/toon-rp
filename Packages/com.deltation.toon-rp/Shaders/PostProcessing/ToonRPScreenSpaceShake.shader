Shader "Hidden/Toon RP/Screen-Space Shake"
{
    SubShader
	{
	    Pass
		{
		    Name "Toon RP Screen-Space Shake"
		    
		    Cull Off ZWrite Off ZTest Always

			HLSLPROGRAM

			//#pragma enable_d3d11_debug_symbols

	        #pragma vertex Vert
		    #pragma fragment Frag
			
			#include "Packages/com.deltation.toon-rp/ShaderLibrary/Common.hlsl"
            #include "Packages/com.deltation.toon-rp/ShaderLibrary/Textures.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
			
			TEXTURE2D_X(_MainTex);
            SAMPLER(sampler_MainTex);

			float _ScreenSpaceShake_Amount;

			float4 Frag(const Varyings IN) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                
                const float2 uv = UnityStereoTransformScreenSpaceTex(IN.texcoord);
                const float shakeAmount = length(uv * 2 - 1);
                const float2 sampleUv = lerp(uv, float2(0.5, 0.5), shakeAmount * _ScreenSpaceShake_Amount);
                float4 color = SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, sampleUv);
                return color;
            }

			ENDHLSL
		}
	}
}