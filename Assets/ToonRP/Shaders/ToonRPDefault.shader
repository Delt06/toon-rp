Shader "Toon RP/Default"
{
	Properties
	{
		[MainColor]
		_MainColor ("Color", Color) = (1, 1, 1, 1)
		[MainTexture]
		_MainTexture ("Texture", 2D) = "white" {}
	    _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.75)
	    [HDR]
		_SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
	    
	    [Toggle(_RECEIVE_BLOB_SHADOWS)]
	    _ReceiveBlobShadows ("Receive Blob Shadows", Float) = 0
	    
	    [Toggle(_OVERRIDE_RAMP)]
	    _OverrideRamp ("Override Ramp", Float) = 0
	    _OverrideRamp_Threshold ("Threshold", Range(-1, 1)) = 0
	    _OverrideRamp_SpecularThreshold ("Specular Threshold", Range(-1, 1)) = 0.995
	    
	    _OverrideRamp_Smoothness ("Smoothness", Range(0, 1)) = 0.083
	    _OverrideRamp_SpecularSmoothness ("Specular Smoothness", Range(0, 1)) = 0.005
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
	    
	    HLSLINCLUDE

	    // Require variable-length loops
		#pragma target 3.5

	    #pragma vertex VS
		#pragma fragment PS
	    
	    ENDHLSL

		Pass
		{
		    Name "Toon RP Forward"
			Tags{ "LightMode" = "ToonRPForward" }
			
			HLSLPROGRAM

			#pragma enable_d3d11_debug_symbols

			#pragma multi_compile_fog

			// Global Ramp
			#pragma multi_compile_fragment _ _TOON_RP_GLOBAL_RAMP_CRISP

			// Shadows
			#pragma multi_compile _ _TOON_RP_DIRECTIONAL_SHADOWS _TOON_RP_BLOB_SHADOWS
			#pragma multi_compile_fragment _ _TOON_RP_SHADOWS_RAMP_CRISP

			// SSAO
			#pragma multi_compile_fragment _ _TOON_RP_SSAO _TOON_RP_SSAO_PATTERN

			// Per-Material
			#pragma shader_feature_local_fragment _OVERRIDE_RAMP 
			#pragma shader_feature_local_fragment _RECEIVE_BLOB_SHADOWS 

			#include "ToonRPDefaultForwardPass.hlsl"
			
			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Shadow Caster"
			Tags{ "LightMode" = "ShadowCaster" }
		    
		    ColorMask RG
			
			HLSLPROGRAM

			#pragma enable_d3d11_debug_symbols

			#include "ToonRPDefaultShadowCasterPass.hlsl"
			
			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Depth Only"
			Tags{ "LightMode" = "ToonRPDepthOnly" }
		    
		    ColorMask 0
			
			HLSLPROGRAM

			#pragma enable_d3d11_debug_symbols

			#include "ToonRPDefaultDepthOnlyPass.hlsl"
			
			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Depth Normals"
			Tags{ "LightMode" = "ToonRPDepthNormals" }
		    
		    ColorMask RGB
			
			HLSLPROGRAM

			#pragma enable_d3d11_debug_symbols

			#include "ToonRPDefaultDepthNormalsPass.hlsl"
			
			ENDHLSL
		}
	}
    
    CustomEditor "ToonRP.Editor.ShaderGUI.ToonRpDefaultShaderGui"
}