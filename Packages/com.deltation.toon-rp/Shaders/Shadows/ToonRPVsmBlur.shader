Shader "Hidden/Toon RP/VSM Blur"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
	    // The mesh of Custom Blit Z to the far clip value
	    // We blur only if the shadowmap value is NOT the far clip value (i.e., clear value)
	    ZTest NotEqual
		ZWrite Off
		ColorMask RG
	    
        HLSLINCLUDE

        //#pragma enable_d3d11_debug_symbols

	    #pragma vertex VS
		#pragma fragment PS
        
        #include "../../ShaderLibrary/CustomBlit.hlsl"
        #include "../../ShaderLibrary/Textures.hlsl"

        float _ToonRP_VSM_BlurScatter;
        
	    ENDHLSL

		Pass
		{
		    Name "Toon RP VSM Gaussian Blur (Horizontal)"
			
			HLSLPROGRAM

			#pragma multi_compile_local_fragment _ _TOON_RP_VSM_BLUR_HIGH_QUALITY
            #pragma multi_compile_local_fragment _ _TOON_RP_VSM_BLUR_EARLY_BAIL

			#include "ToonRPGaussianBlur.hlsl"

			TEXTURE2D(_ToonRP_DirectionalShadowAtlas);
			SAMPLER(sampler_ToonRP_DirectionalShadowAtlas);
			DECLARE_TEXEL_SIZE(_ToonRP_DirectionalShadowAtlas);

			float2 PS(const v2f IN) : SV_TARGET
            {
                return Blur(
                    TEXTURE2D_ARGS(_ToonRP_DirectionalShadowAtlas, sampler_ToonRP_DirectionalShadowAtlas),
                    _ToonRP_DirectionalShadowAtlas_TexelSize.xy,
                    IN.uv, float2(_ToonRP_VSM_BlurScatter, 0.0f));   
            }

			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP VSM Gaussian Blur (Vertical)"
			
			HLSLPROGRAM

			#pragma multi_compile_local_fragment _ _TOON_RP_VSM_BLUR_HIGH_QUALITY
            #pragma multi_compile_local_fragment _ _TOON_RP_VSM_BLUR_EARLY_BAIL

			#include "ToonRPGaussianBlur.hlsl"

			TEXTURE2D(_ToonRP_DirectionalShadowAtlas_Temp);
			SAMPLER(sampler_ToonRP_DirectionalShadowAtlas_Temp);
			DECLARE_TEXEL_SIZE(_ToonRP_DirectionalShadowAtlas_Temp);

			float2 PS(const v2f IN) : SV_TARGET
            {
                return Blur(
                    TEXTURE2D_ARGS(_ToonRP_DirectionalShadowAtlas_Temp, sampler_ToonRP_DirectionalShadowAtlas_Temp),
                    _ToonRP_DirectionalShadowAtlas_Temp_TexelSize.xy,
                    IN.uv, float2(0.0f, _ToonRP_VSM_BlurScatter));   
            }

			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP VSM Box Blur"
			
			HLSLPROGRAM

			TEXTURE2D(_ToonRP_DirectionalShadowAtlas_Temp);
			SAMPLER(sampler_ToonRP_DirectionalShadowAtlas_Temp);
			DECLARE_TEXEL_SIZE(_ToonRP_DirectionalShadowAtlas_Temp);

			float2 Blur(TEXTURE2D_PARAM(tex, texSampler), const float2 texelSize, const float2 uv)
			{
                float2 result = 0;
			    
                result += SAMPLE_TEXTURE2D(tex, texSampler, uv + float2(0.5, 0.5) * texelSize).rg;
                result += SAMPLE_TEXTURE2D(tex, texSampler, uv + float2(-0.5, 0.5) * texelSize).rg;
                result += SAMPLE_TEXTURE2D(tex, texSampler, uv + float2(0.5, -0.5) * texelSize).rg;
                result += SAMPLE_TEXTURE2D(tex, texSampler, uv + float2(-0.5, -0.5) * texelSize).rg;
			    
                return result * 0.25;
			}

			float2 PS(const v2f IN) : SV_TARGET
            {
                const float2 texelSize = _ToonRP_DirectionalShadowAtlas_Temp_TexelSize.xy * _ToonRP_VSM_BlurScatter;
                const float2 uv = IN.uv;
                return Blur(
                    TEXTURE2D_ARGS(_ToonRP_DirectionalShadowAtlas_Temp, sampler_ToonRP_DirectionalShadowAtlas_Temp),
                    texelSize, uv
                    );
            }

			ENDHLSL
		}
	}
}