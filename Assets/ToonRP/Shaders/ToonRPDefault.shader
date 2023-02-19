Shader "Toon RP/Default"
{
	Properties
	{
		[MainColor]
		_MainColor ("Color", Color) = (1, 1, 1, 1)
		[MainTexture]
		_MainTexture ("Texture", 2D) = "white" {}
	    _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.75)
	    [HDR]
		_SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			Tags{"LightMode" = "ToonRPForward"}
			
			HLSLPROGRAM

			// Require variable-length loops
			#pragma target 3.5

			#pragma multi_compile_fragment _ _TOON_RP_GLOBAL_RAMP_CRISP
			
			#pragma vertex VS
			#pragma fragment PS

			#include "../ShaderLibrary/Common.hlsl"
			#include "../ShaderLibrary/Lighting.hlsl"
			#include "../ShaderLibrary/Ramp.hlsl"
			#include "../ShaderLibrary/Textures.hlsl"

			struct appdata
			{
				float3 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float3 normalWs : NORMAL_WS;
			    float3 positionWs : POSITION_WS;
				float4 positionCs : SV_POSITION;
			};

			CBUFFER_START(UnityPerMaterial)
				float4 _MainColor;
				DECLARE_TILING_OFFSET(_MainTexture)
			    float4 _ShadowColor;
			    float3 _SpecularColor;
			CBUFFER_END

			TEXTURE2D(_MainTexture);
			SAMPLER(sampler_MainTexture);
			
			v2f VS(const appdata IN)
			{
				v2f OUT;
			    
			    OUT.uv = APPLY_TILING_OFFSET(IN.uv, _MainTexture);
			    OUT.normalWs = TransformObjectToWorldNormal(IN.normal);

                const float3 positionWs = TransformObjectToWorld(IN.vertex);
			    OUT.positionWs = positionWs;
				OUT.positionCs = TransformWorldToHClip(positionWs);
				
				return OUT;
			}

			float ComputeNDotH(const float3 viewDirectionWs, const float3 normalWs, const float3 lightDirectionWs)
            {
                const float3 halfVector = normalize(viewDirectionWs + lightDirectionWs);
                return dot(normalWs, halfVector);
            }
			
			float4 PS(const v2f IN) : SV_TARGET
			{
				const float3 normalWs = normalize(IN.normalWs);
				const Light light = GetMainLight();
				const float nDotL = dot(normalWs, light.direction);
				const float diffuseRamp = ComputeGlobalRamp(nDotL);
				const float3 albedo = _MainColor.rgb * SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, IN.uv).rgb;
				const float3 mixedShadowColor = MixShadowColor(albedo, _ShadowColor);
				const float3 diffuse = light.color * ApplyRamp(albedo, mixedShadowColor, diffuseRamp);

			    const float3 viewDirectionWs = normalize(GetWorldSpaceViewDir(IN.positionWs));
			    const float nDotH = ComputeNDotH(viewDirectionWs, normalWs, light.direction);
			    const float specularRamp = ComputeGlobalRampSpecular(nDotH);
			    const float3 specular = light.color * _SpecularColor * specularRamp;

			    const float3 outputColor = diffuse + specular;
				return float4(outputColor, 1.0f);
			}
			ENDHLSL
		}
	}
}