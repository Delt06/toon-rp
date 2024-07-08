Shader "Toon RP/Default"
{
	Properties
	{
		[MainColor]
		_MainColor ("Color", Color) = (1, 1, 1, 1)
		[MainTexture]
		_MainTexture ("Texture", 2D) = "white" {}
	    [HDR]
        _EmissionColor ("Emission", Color) = (0, 0, 0, 0)
	    
	    [Toggle(_ALPHATEST_ON)]
	    _AlphaClipping ("Alpha Clipping", Float) = 0
	    _AlphaClipThreshold ("Alpha Clip Threshold", Range(0, 1)) = 0.5
	    
	    _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.75)
	    [HDR]
		_SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
	    _SpecularSizeOffset ("Specular Size Offset", Range(-2, 2)) = 0
	    [HDR]
		_RimColor ("Rim Color", Color) = (0, 0, 0, 0)
		_RimSizeOffset ("Rim Size Offset", Range(-2, 2)) = 0
	    
	    [NoScaleOffset]
	    _NormalMap ("Normal Map", 2D) = "bump" {}
	    
	    [Toggle(_RECEIVE_BLOB_SHADOWS)]
	    _ReceiveBlobShadows ("Receive Blob Shadows", Float) = 0
	    
	    [Enum(DELTation.ToonRP.StencilPreset)]
	    _OutlinesStencilLayer ("Stencil Preset", Float) = 0
	    
	    [Toggle(_OVERRIDE_RAMP)]
	    _OverrideRamp ("Override Ramp", Float) = 0
	    _OverrideRamp_Threshold ("Threshold", Range(-1, 1)) = 0
	    _OverrideRamp_SpecularThreshold ("Specular Threshold", Range(-1, 1)) = 0.995
	    _OverrideRamp_RimThreshold ("Rim Threshold", Range(-1, 1)) = 0.5
	    
	    _OverrideRamp_Smoothness ("Smoothness", Range(0, 1)) = 0.083
	    _OverrideRamp_SpecularSmoothness ("Specular Smoothness", Range(0, 2)) = 0.005
	    _OverrideRamp_RimSmoothness ("Rim Smoothness", Range(0, 2)) = 0.1
	    
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
	    
	    _ForwardStencilRef ("Ref", Int) = 0
	    _ForwardStencilReadMask ("Read Mask", Int) = 255
	    _ForwardStencilWriteMask ("Write Mask", Int) = 255
	    [Enum(UnityEngine.Rendering.CompareFunction)]
	    _ForwardStencilComp ("Comp", Float) = 0
	    [Enum(UnityEngine.Rendering.StencilOp)]
	    _ForwardStencilPass ("Pass", Float) = 0
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "ToonRP" }
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
		    Name "Toon RP Forward"
			Tags{ "LightMode" = "ToonRPForward" }
		    
		    Blend [_BlendSrc] [_BlendDst]
		    ZWrite [_ZWrite]
		    
		    Stencil
            {
                Ref [_ForwardStencilRef]
                ReadMask [_ForwardStencilReadMask]
                WriteMask [_ForwardStencilWriteMask]
                Comp [_ForwardStencilComp]
                Pass [_ForwardStencilPass]
            }
			
			HLSLPROGRAM

			#include_with_pragmas "PragmaIncludes/ToonRPDefaultMultiCompileList.hlsl"
			#include_with_pragmas "PragmaIncludes/ToonRPDefaultShaderFeatureList.hlsl"
			#pragma shader_feature_local _NORMAL_MAP

			#define _TOON_LIGHTING_SPECULAR
			#define _RIM
			#define EMISSION
			#include "ToonRPDefaultForwardPass.hlsl"
			
			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Shadow Caster"
			Tags{ "LightMode" = "ShadowCaster" }
		    
		    ColorMask RG
		    ZClip [_ZClip]
			
			HLSLPROGRAM

			#include_with_pragmas "PragmaIncludes/ToonRPDefaultShadowMultiCompileList.hlsl"
			#include_with_pragmas "PragmaIncludes/ToonRPDefaultBaseShaderFeatureList.hlsl"

			#include "ToonRPDefaultInput.hlsl"
			#include "ToonRPDefaultShadowCasterPass.hlsl"
			
			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Depth Only"
			Tags{ "LightMode" = "ToonRPDepthOnly" }
			
			Stencil
            {
                Ref [_ForwardStencilRef]
                ReadMask [_ForwardStencilReadMask]
                WriteMask [_ForwardStencilWriteMask]
                Comp [_ForwardStencilComp]
                Pass [_ForwardStencilPass]
            }
		    
		    ColorMask 0
			
			HLSLPROGRAM

			#include_with_pragmas "PragmaIncludes/ToonRPDefaultBaseMultiCompileList.hlsl"
			#include_with_pragmas "PragmaIncludes/ToonRPDefaultBaseShaderFeatureList.hlsl"

			#include "ToonRPDefaultInput.hlsl"
			#include "ToonRPDefaultDepthOnlyPass.hlsl"
			
			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Depth Normals"
			Tags{ "LightMode" = "ToonRPDepthNormals" }
			
			Stencil
            {
                Ref [_ForwardStencilRef]
                ReadMask [_ForwardStencilReadMask]
                WriteMask [_ForwardStencilWriteMask]
                Comp [_ForwardStencilComp]
                Pass [_ForwardStencilPass]
            }
		    
		    ColorMask RGB
			
			HLSLPROGRAM

			#include_with_pragmas "PragmaIncludes/ToonRPDefaultBaseMultiCompileList.hlsl"
			#include_with_pragmas "PragmaIncludes/ToonRPDefaultBaseShaderFeatureList.hlsl"
			#pragma shader_feature_local _NORMAL_MAP

			#include "ToonRPDefaultInput.hlsl"
			#include "ToonRPDefaultDepthNormalsPass.hlsl"
			
			ENDHLSL
		}

        Pass
		{
		    Name "Toon RP Motion Vectors"
			Tags{ "LightMode" = "ToonRPMotionVectors" }
			
			Stencil
            {
                Ref [_ForwardStencilRef]
                ReadMask [_ForwardStencilReadMask]
                WriteMask [_ForwardStencilWriteMask]
                Comp [_ForwardStencilComp]
                Pass [_ForwardStencilPass]
            }
		    
		    ColorMask RG
			
			HLSLPROGRAM

			#include_with_pragmas "PragmaIncludes/ToonRPDefaultBaseMultiCompileList.hlsl"
			#include_with_pragmas "PragmaIncludes/ToonRPDefaultBaseShaderFeatureList.hlsl"

			#include "ToonRPDefaultInput.hlsl"
			#include "ToonRPDefaultMotionVectorsPass.hlsl"
			
			ENDHLSL
		}

        Pass
        {
            Name "Toon RP Meta"
            Tags{ "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM

            #include_with_pragmas "PragmaIncludes/ToonRPDefaultBaseMultiCompileList.hlsl"
			#include_with_pragmas "PragmaIncludes/ToonRPDefaultBaseShaderFeatureList.hlsl"
            #pragma shader_feature EDITOR_VISUALIZATION

            #define EMISSION

            #include "ToonRPDefaultInput.hlsl"
            #include "ToonRPDefaultMetaPass.hlsl"

            ENDHLSL
        }
	}
    
    CustomEditor "DELTation.ToonRP.Editor.ShaderGUI.ToonRpDefaultShaderGui"
}