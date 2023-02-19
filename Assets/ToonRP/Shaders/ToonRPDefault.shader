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

			#pragma multi_compile_fragment _ _TOON_RP_GLOBAL_RAMP_CRISP

			#include "ToonRPDefaultForwardPass.hlsl"
			
			ENDHLSL
		}
	    
	    Pass
		{
		    Name "Toon RP Shadow Caster"
			Tags{ "LightMode" = "ShadowCaster" }
		    
		    ColorMask RG
			
			HLSLPROGRAM

			#include "ToonRPDefaultShadowCasterPass.hlsl"
			
			ENDHLSL
		}
	}
}