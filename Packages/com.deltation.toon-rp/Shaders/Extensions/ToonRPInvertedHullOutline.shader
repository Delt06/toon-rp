Shader "Hidden/Toon RP/Outline (Inverted Hull)"
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

	    #pragma multi_compile_local_vertex _ _NOISE
	    #pragma multi_compile_local_vertex _ _DISTANCE_FADE
	    #pragma multi_compile_local_vertex _ _VERTEX_COLOR_THICKNESS_R _VERTEX_COLOR_THICKNESS_G _VERTEX_COLOR_THICKNESS_B _VERTEX_COLOR_THICKNESS_A
	    #pragma multi_compile_local_vertex _ _NORMAL_SEMANTIC_UV2 _NORMAL_SEMANTIC_TANGENT
	    #pragma multi_compile_local_vertex _ _FIXED_SCREEN_SPACE_THICKNESS

	    #ifdef _NORMAL_SEMANTIC_UV2
        #define NORMAL_SEMANTIC TEXCOORD2
		#endif // _NORMAL_SEMANTIC_UV2

		#ifdef _NORMAL_SEMANTIC_TANGENT
		#define NORMAL_SEMANTIC TANGENT
		#endif // _NORMAL_SEMANTIC_TANGENT

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