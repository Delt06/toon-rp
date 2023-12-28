Shader "Hidden/Toon RP/Outline (Inverted Hull)"
{
    Properties 
    {
    }
	SubShader
	{
	    HLSLINCLUDE

		#pragma vertex VS
		#pragma fragment PS

		#include_with_pragmas "./PragmaIncludes/ToonRPInvertedHullOutlineMultiCompileList.hlsl"
		
	    ENDHLSL 
	    
	    Pass
		{
		    Name "Toon RP Outline (Inverted Hull)"
			
			HLSLPROGRAM

			#pragma multi_compile_fog
			
			#include "ToonRPInvertedHullOutlineForwardPass.hlsl"
			
			ENDHLSL
		}

        Pass
		{
		    Name "Toon RP Outline (Inverted Hull) Depth Only"
			Tags{ "LightMode" = "ToonRPDepthOnly" }
		    
		    ColorMask 0
			
			HLSLPROGRAM

			#include "ToonRPInvertedHullOutlineDepthOnly.hlsl"
			
			ENDHLSL
		}

        Pass
		{
		    Name "Toon RP Outline (Inverted Hull) Depth Normals"
			Tags{ "LightMode" = "ToonRPDepthNormals" }
		    
		    ColorMask RGB
			
			HLSLPROGRAM

			#include "ToonRPInvertedHullOutlineDepthNormals.hlsl"
			
			ENDHLSL
		}

        Pass
		{
		    Name "Toon RP Outline (Inverted Hull) Motion Vectors"
			Tags{ "LightMode" = "ToonRPMotionVectors" }
		    
		    ColorMask RG
			
			HLSLPROGRAM

			#include "ToonRPInvertedHullOutlineMotionVectors.hlsl"
			
			ENDHLSL
		}
	}
}