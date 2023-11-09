Shader "Hidden/Toon RP/Baked Blob Shadow Blur"
{
	Properties
	{
	}
	SubShader
	{
		ColorMask R
	    
        HLSLINCLUDE

        //#pragma enable_d3d11_debug_symbols

	    #pragma vertex VS
		#pragma fragment PS
        
        #include "../../ShaderLibrary/CustomBlit.hlsl"
        #include "../../ShaderLibrary/Textures.hlsl"
        
	    ENDHLSL

		Pass
		{
		    Name "Toon RP Baked Blob Shadow Blur"
			
			HLSLPROGRAM

			#pragma multi_compile_local_fragment _ _TOON_RP_VSM_BLUR_HIGH_QUALITY
            #pragma multi_compile_local_fragment _ _TOON_RP_VSM_BLUR_EARLY_BAIL

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			DECLARE_TEXEL_SIZE(_MainTex);

			uint _ApplyStepToSourceSamples;
			float2 _Direction;

			const static uint BlurKernelSize = 5;

            const static float BlurOffsets[BlurKernelSize] =
            {
                -3.2307692308f,
                -1.3846153846f,
                0.0f,
                1.3846153846f,
                3.2307692308f
            };

            const static float BlurWeights[BlurKernelSize] =
            {
                0.0702702703f,
                0.3162162162f,
                0.2270270270f,
                0.3162162162f,
                0.0702702703f
            };

			float Blur(TEXTURE2D_PARAM(tex, texSampler), const float2 texelSize, const float2 uv, const float2 direction)
            {
                float value = 0;

                for (uint i = 0; i < BlurKernelSize; ++i)
                {
                    const float2 uvOffset = uv + BlurOffsets[i] * direction * texelSize;
                    float sample = SAMPLE_TEXTURE2D_LOD(tex, texSampler, uvOffset, 0).r;

                    if (_ApplyStepToSourceSamples)
                    {
                        sample = step(0.001f, sample);
                    }

                    value += sample * BlurWeights[i];
                }

                return value;
            }

			float PS(const v2f IN) : SV_TARGET
            {
                return Blur(
                    TEXTURE2D_ARGS(_MainTex, sampler_MainTex),
                    _MainTex_TexelSize.xy,
                    IN.uv, _Direction
                    );   
            }

			ENDHLSL
		}
	}
}