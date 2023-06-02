Shader "Hidden/Toon RP/Off-Screen Transparency"
{
    Properties 
    {
    }
	SubShader
	{
	    HLSLINCLUDE

	    //#pragma enable_d3d11_debug_symbols

		#pragma vertex VS
		#pragma fragment PS

	    ENDHLSL 
	    
	    Pass
		{
		    Name "Toon RP Off-Screen Transparency"
		    
		    ZWrite Off
		    ZTest Off
		    Cull Off
		    Blend [_BlendSrc] [_BlendDst]
			
			HLSLPROGRAM
			
			#include "../../ShaderLibrary/CustomBlit.hlsl"
			#include "../../ShaderLibrary/Textures.hlsl"

			TEXTURE2D(_ToonRP_CompositeTransparency_Color);
			SAMPLER(sampler_ToonRP_CompositeTransparency_Color);
			TEXTURE2D(_Pattern);
			SAMPLER(sampler_Pattern);
			float _PatternHorizontalTiling;
			float _HeightOverWidth;
			float4 _Tint;

            float4 PS(const v2f IN) : SV_TARGET
            {
                float4 color = SAMPLE_TEXTURE2D_LOD(_ToonRP_CompositeTransparency_Color, sampler_ToonRP_CompositeTransparency_Color, IN.uv, 0);
                color.a = 1 - color.a;

                // using "clip" skips blending, which can save some performance 
                clip(color.a - 0.01);

                float2 patternUv = IN.uv;
                patternUv.x *= _PatternHorizontalTiling;
                patternUv.y *= _PatternHorizontalTiling * _HeightOverWidth;
                const float4 patternColor = _Tint * SAMPLE_TEXTURE2D(_Pattern, sampler_Pattern, patternUv);
                float4 resultingColor = color * _Tint * patternColor;
                resultingColor.a = 1 - resultingColor.a;
                return resultingColor;
            }
			
			ENDHLSL
		}
	}
}