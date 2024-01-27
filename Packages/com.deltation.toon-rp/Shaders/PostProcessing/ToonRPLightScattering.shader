Shader "Hidden/Toon RP/Light Scattering"
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

	    #include "../../ShaderLibrary/Common.hlsl"
        #include "../../ShaderLibrary/Textures.hlsl"

		#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
	    
        #include "../../ShaderLibrary/DepthNormals.hlsl"

        TEXTURE2D_X(_MainTex);
	    SAMPLER(sampler_MainTex);
        DECLARE_TEXEL_SIZE(_MainTex);

	    CBUFFER_START(UnityPerMaterial)
		float2 _Center;
		float _Intensity;
		float _Threshold;
		float _BlurWidth;
	    int _NumSamples;
		CBUFFER_END

        float3 SampleSource(const float2 uv)
        {
            return SAMPLE_TEXTURE2D_X_LOD(_MainTex, sampler_MainTex, uv, 0).rgb;
        }

	    float4 Frag(Varyings IN);
	    
	    ENDHLSL
	    
        Pass
		{
		    Name "Toon RP Light Scattering (Compute)"
			
			HLSLPROGRAM

			bool IsSkybox(const float depth)
            {
                const float skyboxDepthValue =
                    #ifdef UNITY_REVERSED_Z
                    0.0f;
                    #else // !UNITY_REVERSED_Z
                    1.0f;
                    #endif // UNITY_REVERSED_Z
                return depth == skyboxDepthValue;
            }

            float4 Frag(const Varyings IN) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                
                // Sample colors
                const float2 uv = UnityStereoTransformScreenSpaceTex(IN.texcoord);
                const float2 ray = uv - _Center;
                
                float3 result = 0.0f;
                
                for (int i = 0; i < _NumSamples; ++i)
                {
                    const float scale = 1 - _BlurWidth * (float(i) / float(_NumSamples - 1));
                    const float2 sampleUv = _Center + ray * scale;
                    
                    const float3 sampleColor = SampleSource(sampleUv);
                    const float sampleLuminance = Luminance(sampleColor);
                    const float depth = SampleDepthTexture(sampleUv);
                    const float mask = IsSkybox(depth) && sampleLuminance > _Threshold;
                    result += mask * sampleColor / _NumSamples;
                }

                return float4(result * _Intensity, 1);
            }

			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Light Scattering (Combine)"
			
			HLSLPROGRAM

			TEXTURE2D_X(_ToonRP_ScatteringTexture);
			SAMPLER(sampler_ToonRP_ScatteringTexture);
			DECLARE_TEXEL_SIZE(_ToonRP_ScatteringTexture);

			float3 SampleScattering(const float2 uv)
			{
			    return SAMPLE_TEXTURE2D_X(_ToonRP_ScatteringTexture, sampler_ToonRP_ScatteringTexture, uv).rgb;
			}

			float3 SampleScatteringBlur(const float2 uv)
			{
			    float3 result = 0.0f;
			    const float2 step = 4 * _MainTex_TexelSize.xy / _ToonRP_NormalsTexture_TexelSize.xy;
                const float2 offset = _ToonRP_NormalsTexture_TexelSize.xy * step;
			    result += SampleScattering(uv + offset * float2(-1, -1));
			    result += SampleScattering(uv + offset * float2(1, -1));
			    result += SampleScattering(uv + offset * float2(-1, 1));
			    result += SampleScattering(uv + offset * float2(1, 1));
			    return result * 0.25f;
			}

            float4 Frag(const Varyings IN) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                
                // Sample colors
                const float2 uv = UnityStereoTransformScreenSpaceTex(IN.texcoord);
                const float3 scattering = SampleScatteringBlur(uv);
                return float4(SampleSource(uv) + scattering, 1);
            }

			ENDHLSL
		}
	}
}