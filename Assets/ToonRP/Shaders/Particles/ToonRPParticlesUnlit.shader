Shader "Toon RP/Particles/Unlit"
{
	Properties
	{
        [MainColor]
        _MainColor ("Color", Color) = (1, 1, 1, 1)
        [MainTexture]
        _MainTexture ("Texture", 2D) = "white" {}
        
        _QueueOffset ("Queue Offset", Float) = 0
	}
	SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "PreviewType" = "Plane" }
		ZWrite Off
		LOD 100
	    
	    HLSLINCLUDE

	    // Require variable-length loops
		#pragma target 3.5

	    #pragma vertex VS
		#pragma fragment PS
	    
	    ENDHLSL

		Pass
		{
		    Name "Toon RP Particles Unlit Forward"
			Tags{ "LightMode" = "ToonRPForward" }
			
			Blend SrcAlpha OneMinusSrcAlpha
			
			HLSLPROGRAM

			#pragma enable_d3d11_debug_symbols

			#pragma multi_compile_fog
			#pragma multi_compile_instancing
			
			#include "ToonRPParticlesUnlitForwardPass.hlsl"
			
			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Particles Unlit Caster"
			Tags{ "LightMode" = "ShadowCaster" }
		    
		    ColorMask RG
			
			HLSLPROGRAM

			#pragma enable_d3d11_debug_symbols

			#pragma multi_compile_instancing

			#include "../ToonRPDefaultShadowCasterPass.hlsl"
			
			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Particles Unlit Only"
			Tags{ "LightMode" = "ToonRPDepthOnly" }
		    
		    ColorMask 0
			
			HLSLPROGRAM

			#pragma enable_d3d11_debug_symbols

			#pragma multi_compile_instancing

			#include "../ToonRPDefaultDepthOnlyPass.hlsl"
			
			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Particles Unlit Normals"
			Tags{ "LightMode" = "ToonRPDepthNormals" }
		    
		    ColorMask RGB
			
			HLSLPROGRAM

			#pragma enable_d3d11_debug_symbols

			#pragma multi_compile_instancing

			#include "../ToonRPDefaultDepthNormalsPass.hlsl"
			
			ENDHLSL
		}
	}
	
	CustomEditor "ToonRP.Editor.ShaderGUI.ToonRpParticlesUnlitShaderGui"
}