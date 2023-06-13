Shader "Hidden/Toon RP/Blit"
{
	Properties
	{
	}
	SubShader
	{
	    ZTest Off
	    ZWrite Off
	    Cull Off
	    
        HLSLINCLUDE

        //#pragma enable_d3d11_debug_symbols

	    #pragma vertex VS
		#pragma fragment PS
        
        #include "../ShaderLibrary/CustomBlit.hlsl"
        #include "../ShaderLibrary/Textures.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        
	    ENDHLSL

		Pass
		{
		    Name "Toon RP Blit"
			
			HLSLPROGRAM

			float4 PS(const v2f IN) : SV_TARGET
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
            }

			ENDHLSL
		}
	}
}