Shader "Hidden/Toon RP/FXAA"
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

	    ENDHLSL
	    
		Pass
		{
		    Name "Toon RP FXAA"

			HLSLPROGRAM

            #include "ToonRPFXAAHighQuality.hlsl"

			ENDHLSL
		}
	    
        Pass
		{
		    Name "Toon RP FXAA (Fast)"
			
			HLSLPROGRAM

			#include "ToonRPFXAALowQuality.hlsl"

			ENDHLSL
		}
	}
}