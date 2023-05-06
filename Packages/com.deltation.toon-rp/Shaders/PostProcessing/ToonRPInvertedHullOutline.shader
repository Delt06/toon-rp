Shader "Hidden/Toon RP/Outline (Inverted Hull)"
{
	SubShader
	{
	    HLSLINCLUDE

	    #pragma enable_d3d11_debug_symbols

		#pragma vertex VS
		#pragma fragment PS

		#pragma multi_compile_fog

	    ENDHLSL 
	    
	    Pass
		{
		    Name "Toon RP Outline (Inverted Hull)"
			
			HLSLPROGRAM
			
			#include "ToonRPInvertedHullOutline.hlsl"
			
			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Outline (Inverted Hull, UV Normals)"
			
			HLSLPROGRAM
			
			#define NORMAL_SEMANTIC TEXCOORD2
			#include "ToonRPInvertedHullOutline.hlsl"
			
			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Outline (Inverted Hull, Tangent Normals)"
			
			HLSLPROGRAM
			
			#define NORMAL_SEMANTIC TANGENT
			#include "ToonRPInvertedHullOutline.hlsl"
			
			ENDHLSL
		}
	}
}