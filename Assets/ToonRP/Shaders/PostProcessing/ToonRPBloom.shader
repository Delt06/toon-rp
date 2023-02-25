Shader "Hidden/Toon RP/Bloom"
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

        #include "../../ShaderLibrary/Common.hlsl"
        #include "../../ShaderLibrary/Textures.hlsl"

        TEXTURE2D(_MainTex);
        DECLARE_TEXEL_SIZE(_MainTex);

        bool _ToonRP_Bloom_UseBicubicUpsampling;

        #define LINEAR_SAMPLER sampler_linear_clamp
        SAMPLER(LINEAR_SAMPLER);
        #define POINT_SAMPLER sampler_point_clamp
        SAMPLER(POINT_SAMPLER);

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
            OUT.positionCs = TransformObjectToHClip(IN.position);
            return OUT;
        }

        
        
	    ENDHLSL

		Pass
		{
		    Name "Toon RP Bloom Horizontal"
			
			HLSLPROGRAM

			float3 BlurHorizontal(const float2 uv)
            {
	            float3 color = 0.0;
                const float offsets[] =
                {
		            -4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0
	            };
                const float weights[] = {
		            0.01621622, 0.05405405, 0.12162162, 0.19459459, 0.22702703,
		            0.19459459, 0.12162162, 0.05405405, 0.01621622
	            };
	            for (int i = 0; i < 9; i++)
	            {
                    const float offset = offsets[i] * 2.0 * _MainTex_TexelSize.x;
		            color += SAMPLE_TEXTURE2D_LOD(_MainTex, LINEAR_SAMPLER, uv + float2(offset, 0.0), 0).rgb * weights[i];
	            }
	            return float4(color, 1.0);
            }

			float4 PS(const v2f IN) : SV_TARGET
            {
                return float4(BlurHorizontal(IN.uv), 1.0f); 
            }

			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Bloom Vertical"
			
			HLSLPROGRAM

			float3 BlurVertical(const float2 uv)
            {
	            float3 color = 0.0;
                const float offsets[] =
                {
		            -3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923
	            };
                const float weights[] =
	            {
		            0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027
	            };
	            for (int i = 0; i < 5; i++)
	            {
                    const float offset = offsets[i] * _MainTex_TexelSize.y;
		            color += SAMPLE_TEXTURE2D_LOD(_MainTex, LINEAR_SAMPLER, uv + float2(0.0, offset), 0).rgb * weights[i];
	            }
	            return float4(color, 1.0);
            }

			float4 PS(const v2f IN) : SV_TARGET
            {
                return float4(BlurVertical(IN.uv), 1.0f); 
            }

			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Bloom Combine"
			
			HLSLPROGRAM

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

			TEXTURE2D(_MainTex2);
			float _ToonRP_Bloom_Intensity;
			bool _ToonRP_Bloom_UsePattern;
			float _ToonRP_Bloom_PatternScale;
			float _ToonRP_Bloom_PatternPower;
			float _ToonRP_Bloom_PatternMultiplier;
			float _ToonRP_Bloom_PatternEdge;

			float ComputePattern(const float2 uv)
			{
			    float aspectRatio = _MainTex_TexelSize.x / _MainTex_TexelSize.y;
                const float2 scale2 = _ToonRP_Bloom_PatternScale * float2(1, aspectRatio);
                float2 patternUv = uv * scale2 - 0.5;
                const float2 gridUv = ceil(patternUv) / scale2;
                patternUv %= 1;
			
                const float3 gridSample = SAMPLE_TEXTURE2D_LOD(_MainTex, LINEAR_SAMPLER, gridUv, 0).rgb;
                const float luminance = Luminance(gridSample);

			    // scale pattern based on how bright the bloom in that area is
                float2 centeredPatternUv = patternUv * 2 - 1;
                centeredPatternUv /= _ToonRP_Bloom_PatternMultiplier * min(pow(luminance, _ToonRP_Bloom_PatternPower), 1);
			
                float patternIntensity = smoothstep(1, _ToonRP_Bloom_PatternEdge, length(centeredPatternUv));
			    // avoid very small details
                patternIntensity *= step(0.05, luminance);
			    return patternIntensity;
			}

			float4 PS(const v2f IN) : SV_TARGET
            {
                float3 source1;
                if (_ToonRP_Bloom_UseBicubicUpsampling)
                {
                    source1 = SampleTexture2DBicubic(TEXTURE2D_ARGS(_MainTex, LINEAR_SAMPLER), IN.uv, _MainTex_TexelSize.zwxy, 1.0, 0.0).rgb;
                }
                else
                {
                    source1 = SAMPLE_TEXTURE2D_LOD(_MainTex, LINEAR_SAMPLER, IN.uv, 0).rgb;    
                }

                if (_ToonRP_Bloom_UsePattern)
                {
                    source1 *= ComputePattern(IN.uv);
                }
                 
                const float3 source2 = SAMPLE_TEXTURE2D_LOD(_MainTex2, POINT_SAMPLER, IN.uv, 0).rgb;
                return float4(source1 * _ToonRP_Bloom_Intensity + source2, 1.0f);
            }

			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Bloom Prefilter"
			
			HLSLPROGRAM

			float4 _ToonRP_Bloom_Threshold;

			float3 ApplyBloomThreshold(float3 color)
			{
                const float brightness = Max3(color.r, color.g, color.b);
	            float soft = brightness + _ToonRP_Bloom_Threshold.y;
	            soft = clamp(soft, 0.0f, _ToonRP_Bloom_Threshold.z);
	            soft = soft * soft * _ToonRP_Bloom_Threshold.w;
	            float contribution = max(soft, brightness - _ToonRP_Bloom_Threshold.x);
	            contribution /= max(brightness, 0.00001f);
	            return color * contribution;
            }
			
			float4 PS(const v2f IN) : SV_TARGET
            {
                float3 color = ApplyBloomThreshold(SAMPLE_TEXTURE2D_LOD(_MainTex, POINT_SAMPLER, IN.uv, 0).rgb);
                return float4(color, 1.0f);
            }

			ENDHLSL
		}
	}
}