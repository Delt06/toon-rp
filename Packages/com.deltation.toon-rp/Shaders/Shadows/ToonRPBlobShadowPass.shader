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

        struct RendererPackedData
        {
            float4 positionSize;
            float4 params;
        };

        #define BATCH_SIZE 512
        #define MAX_PACKED_DATA_SIZE (16 * 1024 / (16 * 2)) // 16k / sizeof(RendererPackedData)

        CBUFFER_START(_ToonRP_BlobShadows_PackedData)
        RendererPackedData _PackedData[MAX_PACKED_DATA_SIZE];
        CBUFFER_END

        float _ToonRP_BlobShadows_Indices[BATCH_SIZE];

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

        void GetVertexData(const uint vertexId, out float4 positionCs, out float2 centeredUv, out RendererPackedData packedData)
		{
		    const uint quadVertexId = vertexId % 4;
		    const uint instanceId = asuint(_ToonRP_BlobShadows_Indices[vertexId / 4]);

		    float2 positionOs;
		    positionOs.x = quadVertexId % 3 == 0 ? -1 : 1;
		    positionOs.y = quadVertexId < 2 ? 1 : -1;
		    centeredUv = positionOs;

		    packedData = _PackedData[instanceId];
		    const float4 positionHalfSize = packedData.positionSize;
		    const float2 positionWs = positionOs * positionHalfSize.zw + positionHalfSize.xy;
		    const float2 screenUv = ComputeBlobShadowCoords(float3(positionWs.x, 0, positionWs.y));
		    positionCs = ScreenUvToHClip(screenUv);
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
                
                RendererPackedData packedData;
                GetVertexData(vertexId, OUT.positionCs, OUT.centeredUv, packedData);
                
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
                
                RendererPackedData packedData;
                float2 centeredUv;
                GetVertexData(vertexId, OUT.positionCs, centeredUv, packedData);

                OUT.params = packedData.params.xyz;
                
                const float rotationRad = UnpackRotation(packedData.params.w);
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

            TEXTURE2D(_ToonRP_BlobShadows_BakedTexturesAtlas);
            SAMPLER(sampler_ToonRP_BlobShadows_BakedTexturesAtlas);

            float4 _ToonRP_BlobShadows_BakedTexturesAtlas_TilingOffsets[64];

            struct v2f
		    {
                float4 positionCs : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f VS(const uint vertexId : SV_VertexID)
            {
                v2f OUT;
                
                float2 centeredUv;
                RendererPackedData packedData;
                GetVertexData(vertexId, OUT.positionCs, centeredUv, packedData);

                const float rotationRad = UnpackRotation(packedData.params.w);
                centeredUv = Rotate(centeredUv, rotationRad);

                float4 tilingOffset = _ToonRP_BlobShadows_BakedTexturesAtlas_TilingOffsets[(uint) packedData.params.x];
                OUT.uv = (centeredUv * 0.5f + 0.5f) * tilingOffset.xy + tilingOffset.zw;

                return OUT;
            }

			float4 PS(const v2f IN) : SV_TARGET
            {
                return SAMPLE_TEXTURE2D(_ToonRP_BlobShadows_BakedTexturesAtlas, sampler_ToonRP_BlobShadows_BakedTexturesAtlas, IN.uv) * _Saturation;
            }

			ENDHLSL
		}
	}
}