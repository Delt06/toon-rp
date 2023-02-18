Shader "Toon RP/Default"
{
	Properties
	{
		[MainColor]
		_MainColor ("Color", Color) = (1, 1, 1, 1)
		[MainTexture]
		_MainTexture ("Texture", 2D) = "white" {}
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
			#include "../ShaderLibrary/Ramp.hlsl"
			#include "../ShaderLibrary/Lighting.hlsl"

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
				float4 vertex : SV_POSITION;
			};

			CBUFFER_START(UnityPerMaterial)
				float4 _MainColor;
				DECLARE_TILING_OFFSET(_MainTexture)
			CBUFFER_END

			TEXTURE2D(_MainTexture);
			SAMPLER(sampler_MainTexture);
			
			v2f VS(const appdata IN)
			{
				v2f OUT;
				OUT.vertex = TransformObjectToHClip(IN.vertex);
				OUT.normalWs = TransformObjectToWorldNormal(IN.normal);
				OUT.uv = APPLY_TILING_OFFSET(IN.uv, _MainTexture);
				return OUT;
			}
			
			float4 PS(const v2f IN) : SV_TARGET
			{
				const float3 normalWs = normalize(IN.normalWs);
				const Light light = GetMainLight();
				const float nDotL = dot(normalWs, light.direction);
				const float ramp = ComputeGlobalRamp(nDotL);
				const float3 albedo = _MainColor.rgb * SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, IN.uv).rgb;
				const float3 mixedShadowColor = MixGlobalShadowColor(albedo);

				const float3 diffuseColor = light.color * ApplyRamp(albedo, mixedShadowColor, ramp);
				return float4(diffuseColor, 1.0f);
			}
			ENDHLSL
		}
	}
}