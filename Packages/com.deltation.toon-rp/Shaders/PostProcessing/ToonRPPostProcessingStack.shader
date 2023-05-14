Shader "Hidden/Toon RP/Post-Processing Stack"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
	    HLSLINCLUDE

	    #pragma enable_d3d11_debug_symbols

	    #pragma multi_compile_local_fragment _ _FXAA_LOW _FXAA_HIGH

	    #pragma vertex VS
		#pragma fragment PS

	    ENDHLSL
	    
		Pass
		{
		    Name "Toon RP Post-Processing Stack"

			HLSLPROGRAM

			#if defined(_FXAA_LOW) || defined(_FXAA_HIGH)
			#define _FXAA
			#endif // _FXAA_LOW || _FXAA_HIGH

            #include "ToonRPPostProcessingStack.hlsl"

			ENDHLSL
		}
	}
}