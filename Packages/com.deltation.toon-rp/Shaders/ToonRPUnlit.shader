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
	    
	    [Enum(DELTation.ToonRP.StencilPreset)]
	    _OutlinesStencilLayer ("Stencil Preset", Float) = 0
	    
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
                ReadMask [_ForwardStencilReadMask]
                WriteMask [_ForwardStencilWriteMask]
                Comp [_ForwardStencilComp]
                Pass [_ForwardStencilPass]
            }
			
			HLSLPROGRAM

			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			// Per-Material
			#pragma shader_feature_local _ALPHATEST_ON
			#pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON

			// Bug workaround: stencil might not be set if don't create a separate shader variant for outlines
			#pragma shader_feature_local_vertex _STENCIL_OVERRIDE

			#define UNLIT
			#include "ToonRPDefaultForwardPass.hlsl"
			
			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Shadow Caster (Unlit)"
			Tags{ "LightMode" = "ShadowCaster" }
		    
		    ColorMask RG
		    ZClip [_ZClip]
			
			HLSLPROGRAM

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
		    Name "Toon RP Depth Only (Unlit)"
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

			#pragma multi_compile_instancing

			// Per-Material
			#pragma shader_feature_local _ALPHATEST_ON

			#include "ToonRPUnlitInput.hlsl"
			#include "ToonRPDefaultDepthOnlyPass.hlsl"
			
			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Depth Normals (Unlit)"
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

			#pragma multi_compile_instancing

			// Per-Material
			#pragma shader_feature_local _ALPHATEST_ON

			#include "ToonRPUnlitInput.hlsl"
			#include "ToonRPDefaultDepthNormalsPass.hlsl"
			
			ENDHLSL
		}

        Pass
		{
		    Name "Toon RP Motion Vectors (Unlit)"
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

			#pragma multi_compile_instancing

			// Per-Material
			#pragma shader_feature_local _ALPHATEST_ON

			#include "ToonRPUnlitInput.hlsl"
			#include "ToonRPDefaultMotionVectorsPass.hlsl"
			
			ENDHLSL
		}

        Pass
        {
            Name "Toon RP Meta (Unlit)"
            Tags{ "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM

            #pragma multi_compile_instancing

			// Per-Material
			#pragma shader_feature_local _ALPHATEST_ON
            
            #pragma shader_feature EDITOR_VISUALIZATION

            #define META_SIMULATE_EMISSION

            #include "ToonRPUnlitInput.hlsl"
            #include "ToonRPDefaultMetaPass.hlsl"

            ENDHLSL
        }
	}
    
    CustomEditor "DELTation.ToonRP.Editor.ShaderGUI.ToonRpUnlitShaderGui"
}