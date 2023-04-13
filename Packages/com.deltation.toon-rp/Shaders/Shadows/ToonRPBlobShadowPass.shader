Shader "Hidden/Toon RP/Blob Shadow Pass"
{
	Properties
	{
	    _Saturation ("Saturation", Float) = 1
        _SrcBlend ("Src Blend", Float) = 0
        _DstBlend ("Src Blend", Float) = 0
        _BlendOp ("Blend Op", Float) = 0
    }
	SubShader
	{
        Pass
		{
		    Name "Shadow Caster"
		    
            ColorMask R
		    Cull Off
        
            Blend [_SrcBlend] [_DstBlend]
            BlendOp [_BlendOp]
		    
			HLSLPROGRAM

			#pragma enable_d3d11_debug_symbols
			
	        #pragma vertex VS
		    #pragma fragment PS
			
            #include "../../ShaderLibrary/Common.hlsl"

			CBUFFER_START(UnityPerMaterial)
			float _Saturation;
			CBUFFER_END

            struct appdata
            {
                float2 position : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
		    {
                float4 positionCs : SV_POSITION;
                float2 centeredUV : TEXCOORD0;
            };

            v2f VS(const appdata IN)
            {
                v2f OUT;

                OUT.centeredUV = IN.uv;
                OUT.positionCs = float4(IN.position, 0.5f, 1);
                return OUT;
            }

			float4 PS(const v2f IN) : SV_TARGET
            {
                return saturate(1.0f - length(IN.centeredUV)) * _Saturation; 
            }

			ENDHLSL
		}
	}
}