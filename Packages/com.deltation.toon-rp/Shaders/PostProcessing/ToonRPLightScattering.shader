Shader "Hidden/Toon RP/Light Scattering"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
	    HLSLINCLUDE

	    #pragma enable_d3d11_debug_symbols

	    #pragma vertex VS
		#pragma fragment PS

		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

        #include "../../ShaderLibrary/Common.hlsl"
        #include "../../ShaderLibrary/Textures.hlsl"
        #include "../../ShaderLibrary/DepthNormals.hlsl"

        TEXTURE2D(_MainTex);
	    SAMPLER(sampler_MainTex);
        DECLARE_TEXEL_SIZE(_MainTex);

	    CBUFFER_START(UnityPerMaterial)
		float2 _Center;
		float _Intensity;
		float _Threshold;
		float _BlurWidth;
	    int _NumSamples;
		CBUFFER_END

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

        float3 SampleSource(const float2 uv)
        {
            return SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, 0);
        }
	    
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

            float4 PS(const v2f IN) : SV_TARGET
            {
                // Sample colors
                const float2 uv = IN.uv;
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
		    
		    Blend One One
			
			HLSLPROGRAM

			TEXTURE2D(_ToonRP_ScatteringTexture);
			SAMPLER(sampler_ToonRP_ScatteringTexture);
			DECLARE_TEXEL_SIZE(_ToonRP_ScatteringTexture);

			float3 SampleScattering(const float2 uv)
			{
			    return SAMPLE_TEXTURE2D(_ToonRP_ScatteringTexture, sampler_ToonRP_ScatteringTexture, uv);
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

            float4 PS(const v2f IN) : SV_TARGET
            {
                const float2 uv = IN.uv;
                const float3 scattering = SampleScatteringBlur(uv);
                return float4(scattering, 1);
            }

			ENDHLSL
		}
	}
}