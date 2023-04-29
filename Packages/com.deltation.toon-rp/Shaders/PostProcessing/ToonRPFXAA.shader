Shader "Hidden/Toon RP/FXAA"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
	    // https://www.geeks3d.com/20110405/fxaa-fast-approximate-anti-aliasing-demo-glsl-opengl-test-radeon-geforce/3/
		Pass
		{
		    Name "Toon RP FXAA"
			
			HLSLPROGRAM

			#pragma enable_d3d11_debug_symbols

	        #pragma vertex VS
		    #pragma fragment PS

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            #include "../../ShaderLibrary/Common.hlsl"
            #include "../../ShaderLibrary/Textures.hlsl"

            TEXTURE2D(_MainTex);
            DECLARE_TEXEL_SIZE(_MainTex);

            float _FixedContrastThreshold;
            float _RelativeContrastThreshold;
            float _SubpixelBlendingFactor;

            #define LINEAR_SAMPLER sampler_linear_clamp
            SAMPLER(LINEAR_SAMPLER);

            struct appdata
            {
                float3 position : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
		    {
                float4 positionCs : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f VS(const appdata IN)
            {
                v2f OUT;
                OUT.uv = IN.uv;
                OUT.positionCs = TransformObjectToHClip(IN.position);;
                return OUT;
            }

			float3 SampleSource(const float2 uv)
            {
                return SAMPLE_TEXTURE2D_LOD(_MainTex, LINEAR_SAMPLER, uv, 0);
            }

			float3 SampleSource(const float2 uv, const float2 pixelOffset)
            {
                const float2 offsetUv = uv + pixelOffset * _MainTex_TexelSize.xy;
                return SAMPLE_TEXTURE2D_LOD(_MainTex, LINEAR_SAMPLER, offsetUv, 0);
            }

			static const float FXAA_SPAN_MAX = 8.0;
            static const float FXAA_REDUCE_MUL = 1.0/8.0;
            static const float FXAA_REDUCE_MIN = 1.0/128.0;

			float4 PS(const v2f IN) : SV_TARGET
            {
                // Sample colors
                const float2 uv = IN.uv;
                const float3 colorNw = SampleSource(uv);
                const float3 colorNe = SampleSource(uv, float2(1, 0));
                const float3 colorSw = SampleSource(uv, float2(0, 1));
                const float3 colorSe = SampleSource(uv, float2(1, 1));

                // Compute the luminance of samples
                const float lumaNw = Luminance(colorNw);
                const float lumaNe = Luminance(colorNe);
                const float lumaSw = Luminance(colorSw);
                const float lumaSe = Luminance(colorSe);
                
                const float lumaMin = min(lumaNw, min(lumaNe, min(lumaSw, lumaSe)));
                const float lumaMax = max(lumaNw, max(lumaNe, max(lumaSw, lumaSe)));

                // Find the direction along which to make the final samples
                float2 dir;
                dir.x = -((lumaNw + lumaNe) - (lumaSw + lumaSe));
                dir.y = ((lumaNw + lumaSw) - (lumaNe + lumaSe));

                const float dirReduce = max(
                    (lumaNw + lumaNe + lumaSw + lumaSe) * 0.25f * FXAA_REDUCE_MUL,
                    FXAA_REDUCE_MIN
                    );
                const float rcpDirMin = 1.0 / (min(abs(dir.x), abs(dir.y)) + dirReduce);
                dir = clamp(dir * rcpDirMin, -FXAA_SPAN_MAX, FXAA_SPAN_MAX) * _MainTex_TexelSize.xy;

                // Sample along the direction
                float3 colorA = 0.5f * (SampleSource(uv + dir * (1.0f/3.0f - 0.5f)) + SampleSource(uv + dir * (2.0f/3.0f - 0.5f)));
                float3 colorB = colorA * 0.5f + 0.25f * (
                    SampleSource(uv + dir * (-0.5)) + SampleSource(uv + dir * 0.5)
                    );
                const float lumaB = Luminance(colorB);
                if (lumaB < lumaMin || lumaB > lumaMax)
                {
                    return float4(colorA, 1.0);
                }
                    
                return float4(colorB, 1.0);
            }

			ENDHLSL
		}
	}
}