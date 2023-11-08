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
	    ColorMask R
		Cull Off
    
        Blend [_SrcBlend] [_DstBlend]
        BlendOp [_BlendOp]
        
        HLSLINCLUDE

        //#pragma enable_d3d11_debug_symbols
		
        #include "../../ShaderLibrary/Common.hlsl"

		CBUFFER_START(UnityPerMaterial)
		float _Saturation;
		CBUFFER_END

        float2 Rotate(const float2 value, const float angleRad)
		{
            const float2x2 rotationMatrix = float2x2(cos(angleRad), -sin(angleRad), sin(angleRad), cos(angleRad));
		    return mul(rotationMatrix, value);
		}
        
        ENDHLSL
            
        Pass
		{
		    Name "Blob Shadow Caster (Circle)"
		    
			HLSLPROGRAM

            #pragma vertex VS
		    #pragma fragment PS

            struct appdata
            {
                float2 position : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
		    {
                float4 positionCs : SV_POSITION;
                float2 centeredUv : TEXCOORD0;
            };

            v2f VS(const appdata IN)
            {
                v2f OUT;

                OUT.positionCs = float4(IN.position, 0.5f, 1);
                OUT.centeredUv = IN.uv;
                
                return OUT;
            }

			float4 PS(const v2f IN) : SV_TARGET
            {
                return saturate(1.0f - length(IN.centeredUv)) * _Saturation; 
            }

			ENDHLSL
		}

        Pass
		{
		    Name "Blob Shadow Caster (Square)"
		    
			HLSLPROGRAM

            #pragma vertex VS
		    #pragma fragment PS

            struct appdata
            {
                float2 position : POSITION;
                float4 vertexColor : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
		    {
                float4 positionCs : SV_POSITION;
                float3 params : PARAMS; // x = halfSize, y = cornerRadius
                float2 centeredUv : TEXCOORD0;
            };

            v2f VS(const appdata IN)
            {
                v2f OUT;

                OUT.positionCs = float4(IN.position, 0.5f, 1);
                OUT.params = IN.vertexColor.xyz;
                
                const float rotationRad = IN.vertexColor.w * 2.0f * PI;
                OUT.centeredUv = Rotate(IN.uv, rotationRad);

                return OUT;
            }

            float SdfRoundBox(float2 position, const float2 halfSize, const float cornerRadius)
            {
                position = abs(position) - halfSize + cornerRadius;
                return length(max(position, 0.0)) + min(max(position.x, position.y), 0.0) - cornerRadius;
            }

			float4 PS(const v2f IN) : SV_TARGET
            {
                const float2 halfSize = IN.params.xy;
                const float cornerRadius = IN.params.z;
                return saturate(-SdfRoundBox(IN.centeredUv, halfSize, cornerRadius)) * _Saturation; 
            }

			ENDHLSL
		}
	}
}