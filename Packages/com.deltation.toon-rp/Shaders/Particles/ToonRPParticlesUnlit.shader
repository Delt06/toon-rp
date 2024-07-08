Shader "Toon RP/Particles/Unlit"
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
        
        [Enum(DELTation.ToonRP.Editor.ShaderGUI.ShaderEnums.SurfaceType)]
        _SurfaceType ("Surface Type", Float) = 1
        [Enum(DELTation.ToonRP.ToonBlendMode)]
        _BlendMode ("Blend Mode", Float) = 0
        _BlendSrc ("Blend Src", Float) = 5
        _BlendDst ("Blend Dst", Float) = 10
        _ZWrite ("ZWrite", Float) = 0
        [Enum(DELTation.ToonRP.Editor.ShaderGUI.ShaderEnums.RenderFace)]
        _RenderFace ("Render Face", Float) = 2
	    
	    [Toggle]
	    _SoftParticles("Soft Particles", Float) = 0
	    _SoftParticlesDistance ("Soft Particles Distance", Range(0, 10)) = 0
	    _SoftParticlesRange ("Soft Particles Range", Range(0.01, 10)) = 1
        _QueueOffset ("Queue Offset", Float) = 0
        
	}
	SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "PreviewType" = "Plane" "RenderPipeline" = "ToonRP" }
		ZWrite [_ZWrite]
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
		    Name "Toon RP Particles Unlit Forward"
			Tags{ "LightMode" = "ToonRPForward" }
			
			Blend [_BlendSrc] [_BlendDst]
			
			HLSLPROGRAM

			#pragma multi_compile_fog
			#pragma multi_compile_instancing
			
			// Per-Material
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local_fragment _SOFT_PARTICLES
			
			#include "ToonRPParticlesUnlitForwardPass.hlsl"
			
			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Particles Unlit Shadow Caster"
			Tags{ "LightMode" = "ShadowCaster" }
		    
		    ColorMask RG
		    ZClip [_ZClip]
			
			HLSLPROGRAM

			#pragma multi_compile_instancing
			
			// Per-Material
            #pragma shader_feature_local _ALPHATEST_ON

            #include "ToonRPParticlesUnlitInput.hlsl"
			#include "../ToonRPDefaultShadowCasterPass.hlsl"
			
			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Particles Unlit Only"
			Tags{ "LightMode" = "ToonRPDepthOnly" }
		    
		    ColorMask 0
			
			HLSLPROGRAM

			#pragma multi_compile_instancing
			
			// Per-Material
            #pragma shader_feature_local _ALPHATEST_ON

            #include "ToonRPParticlesUnlitInput.hlsl"
			#include "../ToonRPDefaultDepthOnlyPass.hlsl"
			
			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Particles Unlit Normals"
			Tags{ "LightMode" = "ToonRPDepthNormals" }
		    
		    ColorMask RGB
			
			HLSLPROGRAM

			#pragma multi_compile_instancing
			
			// Per-Material
            #pragma shader_feature_local _ALPHATEST_ON

            #include "ToonRPParticlesUnlitInput.hlsl"
			#include "../ToonRPDefaultDepthNormalsPass.hlsl"
			
			ENDHLSL
		}

        Pass
		{
		    Name "Toon RP Particles Motion Vectors"
			Tags{ "LightMode" = "ToonRPMotionVectors" }
		    
		    ColorMask RG
			
			HLSLPROGRAM

			#pragma multi_compile_instancing

			// Per-Material
			#pragma shader_feature_local _ALPHATEST_ON

			#include "ToonRPParticlesUnlitInput.hlsl"
			#include "../ToonRPDefaultMotionVectorsPass.hlsl"
			
			ENDHLSL
		}
	}
	
	CustomEditor "DELTation.ToonRP.Editor.ShaderGUI.ToonRpParticlesUnlitShaderGui"
}