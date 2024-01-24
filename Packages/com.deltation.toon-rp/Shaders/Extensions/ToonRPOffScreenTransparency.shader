Shader "Hidden/Toon RP/Off-Screen Transparency"
{
    Properties 
    {
    }
	SubShader
	{
	    HLSLINCLUDE

	    //#pragma enable_d3d11_debug_symbols

	    #include "../../ShaderLibrary/Common.hlsl"
		#include "../../ShaderLibrary/Textures.hlsl"

		#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

		#pragma vertex Vert
		#pragma fragment Frag

	    float4 Frag(Varyings IN);

	    ENDHLSL 
	    
	    Pass
		{
		    Name "Toon RP Off-Screen Transparency"
		    
		    ZWrite Off
		    ZTest Off
		    Cull Off
		    ColorMask RGB
		    Blend [_BlendSrc] [_BlendDst]
			
			HLSLPROGRAM

			TEXTURE2D_X(_ToonRP_CompositeTransparency_Color);
			SAMPLER(sampler_ToonRP_CompositeTransparency_Color);
			TEXTURE2D(_Pattern);
			SAMPLER(sampler_Pattern);
			float _PatternHorizontalTiling;
			float _HeightOverWidth;
			float4 _Tint;

            float4 Frag(const Varyings IN) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                const float2 uv = UnityStereoTransformScreenSpaceTex(IN.texcoord);
                
                float4 color = SAMPLE_TEXTURE2D_X_LOD(_ToonRP_CompositeTransparency_Color, sampler_ToonRP_CompositeTransparency_Color, uv, 0);
                color.a = 1 - color.a;

                // using "clip" skips blending, which can save some performance 
                clip(color.a - 0.01);

                float2 patternUv = uv;
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