Shader "Hidden/Toon RP/VSM Blur"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
	    // The mesh of Custom Blit  Z to the far clip value
	    // We blur only if the shadowmap value is NOT the far clip value (i.e., clear value)
	    ZTest NotEqual
		ZWrite Off
		ColorMask RG
	    
        HLSLINCLUDE

        #pragma enable_d3d11_debug_symbols

	    #pragma vertex VS
		#pragma fragment PS

        #pragma multi_compile_local_fragment _ _TOON_RP_VSM_BLUR_HIGH_QUALITY

        #include "../../ShaderLibrary/Common.hlsl"
        #include "../../ShaderLibrary/Textures.hlsl"

        // https://www.rastergrid.com/blog/2010/09/efficient-gaussian-blur-with-linear-sampling/

#ifdef _TOON_RP_VSM_BLUR_HIGH_QUALITY

        const static uint BlurKernelSize = 5;

        const static float BlurOffsets[BlurKernelSize] =
        {
            -3.2307692308f, -1.3846153846f,
            0.0f,
            1.3846153846f, 3.2307692308f
        };

        const static float BlurWeights[BlurKernelSize] =
        {
            0.0702702703f, 0.3162162162f,
            0.2270270270f,
            0.3162162162f, 0.0702702703f
        };

#else // !_TOON_RP_VSM_BLUR_HIGH_QUALITY

        const static uint BlurKernelSize = 3;

        const static float BlurOffsets[BlurKernelSize] =
        {
            -1.72027972039f / 2,
            0.0f,
            1.72027972039f / 2
        };

        const static float BlurWeights[BlurKernelSize] =
        {
            0.3864864865f, 0.2270270270f, 0.3864864865f,
        };

#endif // _TOON_RP_VSM_BLUR_HIGH_QUALITY

        #include "../../ShaderLibrary/CustomBlit.hlsl"

        float2 Blur(TEXTURE2D_PARAM(tex, texSampler), float2 texelSize, const float2 uv, const float2 direction)
        {
            float2 value = 0;

            for (uint i = 0; i < BlurKernelSize; ++i)
            {
                const float2 uvOffset = uv + direction * BlurOffsets[i] * texelSize;
                value += SAMPLE_TEXTURE2D(tex, texSampler, uvOffset).rg * BlurWeights[i];
            }

            return value;
        }
        
	    ENDHLSL

		Pass
		{
		    Name "Toon RP VSM Gaussian Blur (Horizontal)"
			
			HLSLPROGRAM

			TEXTURE2D(_ToonRP_DirectionalShadowAtlas);
			SAMPLER(sampler_ToonRP_DirectionalShadowAtlas);
			DECLARE_TEXEL_SIZE(_ToonRP_DirectionalShadowAtlas);

			float2 PS(const v2f IN) : SV_TARGET
            {
                return Blur(_ToonRP_DirectionalShadowAtlas, sampler_ToonRP_DirectionalShadowAtlas,
                    _ToonRP_DirectionalShadowAtlas_TexelSize.xy,
                    IN.uv, float2(1.0f, 0.0f));   
            }

			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP VSM Gaussian Blur (Vertical)"
			
			HLSLPROGRAM

			TEXTURE2D(_ToonRP_DirectionalShadowAtlas_Temp);
			SAMPLER(sampler_ToonRP_DirectionalShadowAtlas_Temp);
			DECLARE_TEXEL_SIZE(_ToonRP_DirectionalShadowAtlas_Temp);

			float2 PS(const v2f IN) : SV_TARGET
            {
                return Blur(
                    _ToonRP_DirectionalShadowAtlas_Temp, sampler_ToonRP_DirectionalShadowAtlas_Temp,
                    _ToonRP_DirectionalShadowAtlas_Temp_TexelSize.xy,
                    IN.uv, float2(0.0f, 1.0f));   
            }

			ENDHLSL
		}
	}
}