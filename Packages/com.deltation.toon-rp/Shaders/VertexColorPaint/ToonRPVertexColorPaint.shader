Shader "Hidden/Vertex Color Paint"
{
	Properties
	{
        _DiffuseIntensity0 ("Diffuse Intensity 0", Range(0, 1)) = 0.25
        _DiffuseIntensity1 ("Diffuse Intensity 1", Range(0, 1)) = 1.0
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry"}
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
			
			HLSLPROGRAM

			//#pragma enable_d3d11_debug_symbols

			#pragma multi_compile_local_vertex _ VIEW_ALPHA

			#pragma multi_compile_fog
			#pragma multi_compile_instancing
			
			#include "ToonRPVertexColorPaintForwardPass.hlsl"
			
			ENDHLSL
		}
    }
}