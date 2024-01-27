Shader "Hidden/Toon RP/Bloom"
{
	Properties
	{
	}
	SubShader
	{
	    Cull Off ZWrite Off ZTest Always
	    
        HLSLINCLUDE

        //#pragma enable_d3d11_debug_symbols

	    #pragma vertex Vert
		#pragma fragment Frag
        
        #include "Packages/com.deltation.toon-rp/ShaderLibrary/Common.hlsl"
        #include "Packages/com.deltation.toon-rp/ShaderLibrary/Textures.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float4 Frag(Varyings IN);

        TEXTURE2D_X(_MainTex);
        SAMPLER(sampler_MainTex);
        DECLARE_TEXEL_SIZE(_MainTex);
        
	    ENDHLSL

		Pass
		{
		    Name "Toon RP Bloom Blur Horizontal"
			
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
		            color += SAMPLE_TEXTURE2D_X_LOD(_MainTex, sampler_MainTex, uv + float2(offset, 0.0), 0).rgb * weights[i];
	            }
                return color;
            }

			float4 Frag(const Varyings IN) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                const float2 uv = UnityStereoTransformScreenSpaceTex(IN.texcoord);
                return float4(BlurHorizontal(uv), 1.0f); 
            }

			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Bloom Blur Vertical"
			
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
		            color += SAMPLE_TEXTURE2D_X_LOD(_MainTex, sampler_MainTex, uv + float2(0.0, offset), 0).rgb * weights[i];
	            }
                return color;
            }

			float4 Frag(const Varyings IN) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                const float2 uv = UnityStereoTransformScreenSpaceTex(IN.texcoord);
                return float4(BlurVertical(uv), 1.0f); 
            }

			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Bloom Combine"
			
			HLSLPROGRAM

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
			#include "Packages/com.deltation.toon-rp/ShaderLibrary/Ramp.hlsl"

			TEXTURE2D_X(_MainTex2);
			float _ToonRP_Bloom_Intensity;
			bool _ToonRP_Bloom_UsePattern;
			float _ToonRP_Bloom_PatternScale;
			float _ToonRP_Bloom_PatternPower;
			float _ToonRP_Bloom_PatternMultiplier;
			float _ToonRP_Bloom_PatternEdge;
			float _ToonRP_Bloom_PatternLuminanceThreshold;
			float _ToonRP_Bloom_PatternDotSizeLimit;
			float _ToonRP_Bloom_PatternBlend;
			float2 _ToonRP_Bloom_PatternFinalIntensityRamp;

			float ComputePattern(const float2 uv)
			{
			    float aspectRatio = _MainTex_TexelSize.x / _MainTex_TexelSize.y;
                const float2 scale2 = _ToonRP_Bloom_PatternScale * float2(1, aspectRatio);
                float2 patternUv = uv * scale2 - 0.5;
                const float2 gridUv = ceil(patternUv) / scale2;
                patternUv %= 1;
			
                const float3 gridSample = SAMPLE_TEXTURE2D_X_LOD(_MainTex, sampler_MainTex, gridUv, 0).rgb;
                const float luminance = Luminance(gridSample);

			    // scale pattern based on how bright the bloom in that area is
                float2 centeredPatternUv = patternUv * 2 - 1;
                centeredPatternUv /= min(_ToonRP_Bloom_PatternDotSizeLimit, _ToonRP_Bloom_PatternMultiplier * min(pow(abs(luminance), _ToonRP_Bloom_PatternPower), 1));

			    // smooth circle in each grid cell
                float patternIntensity = smoothstep(1, _ToonRP_Bloom_PatternEdge, length(centeredPatternUv));
			    // avoid very small details
                patternIntensity *= step(_ToonRP_Bloom_PatternLuminanceThreshold, luminance);
			    return patternIntensity;
			}

			float4 Frag(const Varyings IN) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                const float2 uv = UnityStereoTransformScreenSpaceTex(IN.texcoord);
                
                float3 source1 = SAMPLE_TEXTURE2D_X_LOD(_MainTex, sampler_MainTex, uv, 0).rgb;

                if (_ToonRP_Bloom_UsePattern)
                {
                    source1 *= ComputeRamp(max(source1.r, max(source1.g, source1.b)), _ToonRP_Bloom_PatternFinalIntensityRamp);
                    const float patternStrength = lerp(_ToonRP_Bloom_PatternBlend, 1, ComputePattern(uv));
                    source1 *= patternStrength;
                }
                 
                const float3 source2 = SAMPLE_TEXTURE2D_X_LOD(_MainTex2, sampler_MainTex, uv, 0).rgb;
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
			
			float4 Frag(const Varyings IN) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                const float2 uv = UnityStereoTransformScreenSpaceTex(IN.texcoord);
                float3 color = ApplyBloomThreshold(SAMPLE_TEXTURE2D_X_LOD(_MainTex, sampler_MainTex, uv, 0).rgb);
                return float4(color, 1.0f);
            }

			ENDHLSL
		}
	}
}