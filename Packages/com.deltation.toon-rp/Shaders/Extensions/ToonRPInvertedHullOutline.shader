Shader "Hidden/Toon RP/Outline (Inverted Hull)"
{
    Properties 
    {
        _Thickness("Thickness", Float) = 0
        _Color("Color", Color) = (0, 0, 0, 0)
        _DistanceFade("Distance Fade", Vector) = (0, 0, 0, 0)
        _NoiseFrequency("Noise Frequency", Float) = 0
        _NoiseAmplitude("Noise Amplitude", Float) = 0
    }
	SubShader
	{
	    HLSLINCLUDE

	    //#pragma enable_d3d11_debug_symbols

		#pragma vertex VS
		#pragma fragment PS

		#pragma multi_compile_fog

	    #pragma multi_compile_local_vertex _ _NOISE
	    #pragma multi_compile_local_vertex _ _DISTANCE_FADE
	    #pragma multi_compile_local_vertex _ _VERTEX_COLOR_THICKNESS_R _VERTEX_COLOR_THICKNESS_G _VERTEX_COLOR_THICKNESS_B _VERTEX_COLOR_THICKNESS_A
	    #pragma multi_compile_local_vertex _ _FIXED_SCREEN_SPACE_THICKNESS

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