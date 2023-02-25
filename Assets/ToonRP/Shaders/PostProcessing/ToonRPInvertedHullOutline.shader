Shader "Hidden/Toon RP/Outline (Inverted Hull)"
{
	SubShader
	{
	    Pass
		{
		    Name "Toon RP Outline (Inverted Hull)"
			
			HLSLPROGRAM

			#pragma vertex VS
			#pragma fragment PS

			#include "../../ShaderLibrary/Common.hlsl"

			struct appdata
			{
			    float3 vertex : POSITION;
			    float3 normal : NORMAL;
			};

			struct v2f
			{
			    float4 positionCs : SV_POSITION;
			};

			float _ToonRP_Outline_InvertedHull_Thickness;
			float3 _ToonRP_Outline_InvertedHull_Color;

			v2f VS(const appdata IN)
			{
			    v2f OUT;

			    const float4 positionCs = TransformObjectToHClip(IN.vertex);
			    const float3 normalCs = TransformWorldToHClipDir(TransformObjectToWorldNormal(IN.normal));
			    OUT.positionCs = positionCs + float4(normalCs * _ToonRP_Outline_InvertedHull_Thickness, 0);

			    return OUT;
			}

			float4 PS() : SV_TARGET
			{
			    return float4(_ToonRP_Outline_InvertedHull_Color, 1);
			    
			}

			ENDHLSL
		}
	}
}