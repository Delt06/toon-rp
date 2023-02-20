Shader "Hidden/Toon RP/VSM Blur"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
        HLSLINCLUDE

	    #pragma vertex VS
		#pragma fragment PS

        #pragma multi_compile_local_fragment _ _TOON_RP_VSM_BLUR_HIGH_QUALITY

        #include "../ShaderLibrary/Common.hlsl"
        #include "../ShaderLibrary/Textures.hlsl"

        TEXTURE2D(_MainTex);
        DECLARE_TEXEL_SIZE(_MainTex);

        #define SOURCE_SAMPLER sampler_linear_clamp
        SAMPLER(SOURCE_SAMPLER);

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

        struct appdata
        {
            float2 uv : TEXCOORD0;
            float3 position : POSITION;
        };

        struct v2f
        {
            float2 uv : TEXCOORD0;
            float4 positionCs : SV_POSITION;
        };

        v2f VS(const appdata IN)
        {
            v2f OUT;
            OUT.uv = IN.uv;
            OUT.positionCs = TransformObjectToHClip(IN.position);
            return OUT;
        }

        float2 Blur(const float2 uv, const float2 direction)
        {
            float2 value = 0;
            const float2 texelSize = _MainTex_TexelSize.xy;

            for (uint i = 0; i < BlurKernelSize; ++i)
            {
                const float2 uvOffset = uv + direction * BlurOffsets[i] * texelSize;
                value += SAMPLE_TEXTURE2D(_MainTex, SOURCE_SAMPLER, uvOffset).rg * BlurWeights[i];
            }

            return value;
        }
        
	    ENDHLSL

		Pass
		{
		    Name "Toon RP VSM Gaussian Blur (Horizontal)"
		    ZTest Off
		    ZWrite Off
		    ColorMask RG
			
			HLSLPROGRAM

			float2 PS(const v2f IN) : SV_TARGET
            {
                return Blur(IN.uv, float2(1.0f, 0.0f));   
            }

			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP VSM Gaussian Blur (Vertical)"
		    ZTest Off
		    ZWrite Off
		    ColorMask RG
			
			HLSLPROGRAM

			float2 PS(const v2f IN) : SV_TARGET
            {
                return Blur(IN.uv, float2(0.0f, 1.0f));   
            }

			ENDHLSL
		}
	}
}