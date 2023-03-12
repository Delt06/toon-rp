Shader "Hidden/Toon RP/Outline (Inverted Hull)"
{
	SubShader
	{
	    Pass
		{
		    Name "Toon RP Outline (Inverted Hull)"
			
			HLSLPROGRAM

			#pragma enable_d3d11_debug_symbols

			#pragma vertex VS
			#pragma fragment PS

			#pragma multi_compile_fog

			#include "ToonRPInvertedHullOutline.hlsl"

			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Outline (Inverted Hull, Custom Normals)"
			
			HLSLPROGRAM

			#pragma enable_d3d11_debug_symbols

			#pragma vertex VS
			#pragma fragment PS

			#pragma multi_compile_fog

			#define TOON_RP_USE_TEXCOORD2_NORMALS
			#include "ToonRPInvertedHullOutline.hlsl"

			ENDHLSL
		}
	}
}