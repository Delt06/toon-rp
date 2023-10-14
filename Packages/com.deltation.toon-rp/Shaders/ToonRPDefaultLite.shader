﻿Shader "Toon RP/Default (Lite)"
{
	Properties
	{
		[MainColor]
		_MainColor ("Color", Color) = (1, 1, 1, 1)
		[MainTexture]
		_MainTexture ("Texture", 2D) = "white" {}
	    
	    [Toggle(_ALPHATEST_ON)]
	    _AlphaClipping ("Alpha Clipping", Float) = 0
	    _AlphaClipThreshold ("Alpha Clip Threshold", Range(0, 1)) = 0.5
	    
	    _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.75)
	    
	    [Toggle(_RECEIVE_BLOB_SHADOWS)]
	    _ReceiveBlobShadows ("Receive Blob Shadows", Float) = 0
	    
	    [Enum(DELTation.ToonRP.StencilLayer)]
	    _OutlinesStencilLayer ("Outlines Stencil Layer", Float) = 0
	    
	    [Toggle(_OVERRIDE_RAMP)]
	    _OverrideRamp ("Override Ramp", Float) = 0
	    _OverrideRamp_Threshold ("Threshold", Range(-1, 1)) = 0
	    _OverrideRamp_SpecularThreshold ("Specular Threshold", Range(-1, 1)) = 0.995
	    _OverrideRamp_RimThreshold ("Rim Threshold", Range(-1, 1)) = 0.5
	    
	    _OverrideRamp_Smoothness ("Smoothness", Range(0, 1)) = 0.083
	    _OverrideRamp_SpecularSmoothness ("Specular Smoothness", Range(0, 2)) = 0.005
	    _OverrideRamp_RimSmoothness ("Rim Smoothness", Range(0, 2)) = 0.1
	    
	    [Enum(DELTation.ToonRP.MatcapMode)]
	    _MatcapMode ("Matcap Mode", Float) = 0
	    [NoScaleOffset]
	    _MatcapTexture ("Matcap", 2D) = "black" {}
	    [HDR]
	    _MatcapTint ("Matcap Tint", Color) = (1, 1, 1, 1)
	    _MatcapBlend ("Matcap Blend", Range(0, 1)) = 1
	    
	    [Enum(DELTation.ToonRP.Editor.ShaderGUI.ShaderEnums.SurfaceType)]
        _SurfaceType ("Surface Type", Float) = 0
        [Enum(DELTation.ToonRP.ToonBlendMode)]
        _BlendMode ("Blend Mode", Float) = 0
        _BlendSrc ("Blend Src", Float) = 1
        _BlendDst ("Blend Dst", Float) = 0
        _ZWrite ("ZWrite", Float) = 1
        [Enum(DELTation.ToonRP.Editor.ShaderGUI.ShaderEnums.RenderFace)]
        _RenderFace ("Render Face", Float) = 2
	    
	    _QueueOffset ("Queue Offset", Float) = 0
	    
	    _ForwardStencilRef ("Stencil Ref", Float) = 0
	    _ForwardStencilWriteMask ("Stencil Write Mask", Float) = 0
	    _ForwardStencilComp ("Stencil Comp", Float) = 0
	    _ForwardStencilPass ("Stencil Pass", Float) = 0
	    
	    [Toggle(_FORCE_DISABLE_FOG)]
	    _ForceDisableFog ("Force Disable Fog", Float) = 0
	    
	    [Toggle(_FORCE_DISABLE_ENVIRONMENT_LIGHT)]
	    _ForceDisableEnvironmentLight ("Force Disable Environment Light", Float) = 0
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		Cull [_RenderFace]
		LOD 100
	    
	    HLSLINCLUDE

	    //#pragma enable_d3d11_debug_symbols

	    // Require variable-length loops
		#pragma target 3.5

	    #pragma vertex VS
		#pragma fragment PS
	    
	    ENDHLSL

		Pass
		{
		    Name "Toon RP Forward (Lite)"
			Tags{ "LightMode" = "ToonRPForward" }
		    
		    Blend [_BlendSrc] [_BlendDst]
		    ZWrite [_ZWrite]
		    
		    Stencil
            {
                Ref [_ForwardStencilRef]
                WriteMask [_ForwardStencilWriteMask]
                Comp [_ForwardStencilComp]
                Pass [_ForwardStencilPass]
            }
			
			HLSLPROGRAM

			#include_with_pragmas "PragmaIncludes/ToonRPDefaultMultiCompileList.hlsl"
			#include_with_pragmas "PragmaIncludes/ToonRPDefaultShaderFeatureList.hlsl"
			#pragma shader_feature_local _FORCE_DISABLE_FOG
			#pragma shader_feature_local _FORCE_DISABLE_ENVIRONMENT_LIGHT

			#define DEFAULT_LITE
			#include "ToonRPDefaultForwardPass.hlsl"
			
			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Shadow Caster (Lite)"
			Tags{ "LightMode" = "ShadowCaster" }
		    
		    ColorMask RG
			
			HLSLPROGRAM

			#include_with_pragmas "PragmaIncludes/ToonRPDefaultShadowMultiCompileList.hlsl"
			#include_with_pragmas "PragmaIncludes/ToonRPDefaultBaseShaderFeatureList.hlsl"

			#include "ToonRPDefaultLiteInput.hlsl"
			#include "ToonRPDefaultShadowCasterPass.hlsl"
			
			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Depth Only (Lite)"
			Tags{ "LightMode" = "ToonRPDepthOnly" }
		    
		    ColorMask 0
			
			HLSLPROGRAM

			#include_with_pragmas "PragmaIncludes/ToonRPDefaultBaseMultiCompileList.hlsl"
			#include_with_pragmas "PragmaIncludes/ToonRPDefaultBaseShaderFeatureList.hlsl"

			#include "ToonRPDefaultLiteInput.hlsl"
			#include "ToonRPDefaultDepthOnlyPass.hlsl"
			
			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Depth Normals (Lite)"
			Tags{ "LightMode" = "ToonRPDepthNormals" }
		    
		    ColorMask RGB
			
			HLSLPROGRAM

			#include_with_pragmas "PragmaIncludes/ToonRPDefaultBaseMultiCompileList.hlsl"
			#include_with_pragmas "PragmaIncludes/ToonRPDefaultBaseShaderFeatureList.hlsl"

			#include "ToonRPDefaultLiteInput.hlsl"
			#include "ToonRPDefaultDepthNormalsPass.hlsl"
			
			ENDHLSL
		}

        Pass
		{
		    Name "Toon RP Motion Vectors (Lite)"
			Tags{ "LightMode" = "ToonRPMotionVectors" }
		    
		    ColorMask RG
			
			HLSLPROGRAM

			#include_with_pragmas "PragmaIncludes/ToonRPDefaultBaseMultiCompileList.hlsl"
			#include_with_pragmas "PragmaIncludes/ToonRPDefaultBaseShaderFeatureList.hlsl"

			#include "ToonRPDefaultLiteInput.hlsl"
			#include "ToonRPDefaultMotionVectorsPass.hlsl"
			
			ENDHLSL
		}
	}
    
    CustomEditor "DELTation.ToonRP.Editor.ShaderGUI.ToonRpDefaultLiteShaderGui"
}