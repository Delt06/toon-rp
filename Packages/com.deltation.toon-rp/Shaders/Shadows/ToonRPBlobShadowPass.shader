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
    
        Blend [_SrcBlend] [_DstBlend]
        BlendOp [_BlendOp]
        
        HLSLINCLUDE

        //#pragma enable_d3d11_debug_symbols
		
        #include "../../ShaderLibrary/Common.hlsl"
        #include "../../ShaderLibrary/BlobShadows.hlsl"

        float4 _ToonRP_BlobShadows_Positions[128];
        float4 _ToonRP_BlobShadows_Params[128];

		CBUFFER_START(UnityPerMaterial)
		float _Saturation;
		CBUFFER_END

        float4 ScreenUvToHClip(const float2 screenUv)
		{
		    float4 positionCs = float4(screenUv * 2.0f - 1.0f, 0.5f, 1.0f);

		    #ifdef UNITY_UV_STARTS_AT_TOP
		    positionCs.y *= -1;
		    #endif // UNITY_UV_STARTS_AT_TOP

		    return positionCs;
		}

        void GetVertexData(const uint vertexId, out float4 positionCs, out float2 centeredUv, out uint instanceId)
		{
		    const uint quadVertexId = vertexId % 4;
		    instanceId = vertexId / 4;

		    float2 positionOs;
		    positionOs.x = quadVertexId % 3 == 0 ? -1 : 1;
		    positionOs.y = quadVertexId < 2 ? 1 : -1;
		    centeredUv = positionOs;
		    
		    const float4 positionHalfSize = _ToonRP_BlobShadows_Positions[instanceId];
		    const float2 positionWs = positionOs * positionHalfSize.zw + positionHalfSize.xy;
		    const float2 screenUv = ComputeBlobShadowCoords(float3(positionWs.x, 0, positionWs.y));
		    positionCs = ScreenUvToHClip(screenUv);
		}
        
        float4 GetParams(const uint instanceId)
		{
		    return _ToonRP_BlobShadows_Params[instanceId];
		}

        float UnpackRotation(const float packedRotation)
		{
		    return -packedRotation * 2.0f * PI;
		}

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

            v2f VS(const uint vertexId : SV_VertexID)
            {
                v2f OUT;
                
                uint instanceId;
                GetVertexData(vertexId, OUT.positionCs, OUT.centeredUv, instanceId);
                
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

            struct v2f
		    {
                float4 positionCs : SV_POSITION;
                float3 params : PARAMS; // x = halfSize, y = cornerRadius, z = rotation
                float2 centeredUv : TEXCOORD0;
            };

            v2f VS(const uint vertexId : SV_VertexID)
            {
                v2f OUT;
                
                uint instanceId;
                float2 centeredUv;
                GetVertexData(vertexId, OUT.positionCs, centeredUv, instanceId);

                const float4 params = _ToonRP_BlobShadows_Params[instanceId];
                OUT.params = params.xyz;
                
                const float rotationRad = UnpackRotation(params.w);
                OUT.centeredUv = Rotate(centeredUv, rotationRad);

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

        Pass
		{
		    Name "Blob Shadow Caster (Baked)"
		    
			HLSLPROGRAM

            #pragma vertex VS
		    #pragma fragment PS

            TEXTURE2D(_BakedBlobShadowTexture);
            SAMPLER(sampler_BakedBlobShadowTexture);

            struct v2f
		    {
                float4 positionCs : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f VS(const uint vertexId : SV_VertexID)
            {
                v2f OUT;
                
                uint instanceId;
                float2 centeredUv;
                GetVertexData(vertexId, OUT.positionCs, centeredUv, instanceId);

                const float4 params = _ToonRP_BlobShadows_Params[instanceId];
                const float rotationRad = UnpackRotation(params.w);
                centeredUv = Rotate(centeredUv, rotationRad);
                OUT.uv = centeredUv * 0.5f + 0.5f;

                return OUT;
            }

			float4 PS(const v2f IN) : SV_TARGET
            {
                return SAMPLE_TEXTURE2D(_BakedBlobShadowTexture, sampler_BakedBlobShadowTexture, IN.uv) * _Saturation;
            }

			ENDHLSL
		}
	}
}