Shader "Toon RP/Unlit"
{
	Properties
	{
		[MainColor] [HDR] 
		_MainColor ("Color", Color) = (1, 1, 1, 1)
		[MainTexture]
		_MainTexture ("Texture", 2D) = "white" {}
	    
	    [Toggle(_ALPHATEST_ON)]
	    _AlphaClipping ("Alpha Clipping", Float) = 0
	    _AlphaClipThreshold ("Alpha Clip Threshold", Range(0, 1)) = 0.5
	    
	    [Enum(DELTation.ToonRP.StencilLayer)]
	    _OutlinesStencilLayer ("Outlines Stencil Layer", Float) = 0
	    
	    [Enum(DELTation.ToonRP.Editor.ShaderGUI.ShaderEnums.SurfaceType)]
        _SurfaceType ("Surface Type", Float) = 0
        [Enum(DELTation.ToonRP.Editor.ShaderGUI.ShaderEnums.BlendMode)]
        _BlendMode ("Blend Mode", Float) = 0
        _BlendSrc ("Blend Src", Float) = 1
        _BlendDst ("Blend Dst", Float) = 0
        _ZWrite ("ZWrite", Float) = 1
        [Enum(DELTation.ToonRP.Editor.ShaderGUI.ShaderEnums.RenderFace)]
        _RenderFace ("Render Face", Float) = 2
	    
	    _QueueOffset ("Queue Offset", Float) = 0
	    
	    _ForwardStencilRef ("Stencil Ref", Integer) = 0
	    _ForwardStencilWriteMask ("Stencil Write Mask", Integer) = 0
	    _ForwardStencilComp ("Stencil Comp", Integer) = 0
	    _ForwardStencilPass ("Stencil Pass", Integer) = 0
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		Cull [_RenderFace]
		LOD 100
	    
	    HLSLINCLUDE

	    #pragma vertex VS
		#pragma fragment PS
	    
	    ENDHLSL

		Pass
		{
		    Name "Toon RP Unlit Forward"
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

			#pragma enable_d3d11_debug_symbols

			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			// Per-Material
			#pragma shader_feature_local _ALPHATEST_ON
			#pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON

			// Bug workaround: stencil might not be set if don't create a separate shader variant for outlines
			#pragma shader_feature_local_vertex _HAS_OUTLINES_STENCIL_LAYER

			#define UNLIT
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

			#pragma multi_compile_instancing

			// Shadows
			#pragma multi_compile _ _TOON_RP_VSM

			// Per-Material
			#pragma shader_feature_local _ALPHATEST_ON

			#include "ToonRPUnlitInput.hlsl"
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

			#pragma multi_compile_instancing

			// Per-Material
			#pragma shader_feature_local _ALPHATEST_ON

			#include "ToonRPUnlitInput.hlsl"
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

			#pragma multi_compile_instancing

			// Per-Material
			#pragma shader_feature_local _ALPHATEST_ON
			#pragma shader_feature_local _NORMAL_MAP

			#include "ToonRPUnlitInput.hlsl"
			#include "ToonRPDefaultDepthNormalsPass.hlsl"
			
			ENDHLSL
		}
	}
    
    CustomEditor "DELTation.ToonRP.Editor.ShaderGUI.ToonRpUnlitShaderGui"
}