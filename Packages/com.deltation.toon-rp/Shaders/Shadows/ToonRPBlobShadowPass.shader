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
            uint4 params1;
            uint4 params2;

            float rotation;
            float offsetMultiplier;
            float saturation;
        };

        // OpenGLES 3.0 guarantess GL_MAX_VERTEX_UNIFORM_VECTORS >= 256
        // It is important to pack all per-renderer data into 16 bytes, otherwise we will have to split buffers or reduce the batch size.
        #define BATCH_SIZE 256

        CBUFFER_START(_ToonRP_BlobShadows_PackedData)
        float4 _PackedData[BATCH_SIZE];
        CBUFFER_END

        CBUFFER_START(_ToonRP_BlobShadows_Indices)
        float _Indices[BATCH_SIZE];
        CBUFFER_END

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

        uint4 UnpackBytes(const uint packedValue)
		{
		    return uint4(
		        packedValue & 0xFF,
		        packedValue >> 8 & 0xFF,
		        packedValue >> 16 & 0xFF,
		        packedValue >> 24 & 0xFF
		    );
		}

        float AsUNorm(const uint packedByte)
		{
		    return saturate(packedByte / 255.0f); 
		}

        float AsSNorm(const uint packedByte)
		{
		    return AsUNorm(packedByte) * 2.0f - 1.0f; 
		}

        float2 AsUNorm(const uint2 packedByte)
		{
		    return float2(AsUNorm(packedByte.x), AsUNorm(packedByte.y)); 
		}

        float UnpackRotation(const float packedRotation)
		{
		    return -packedRotation * 2.0f * PI;
		}
 
        void UnpackParams(const uint2 params, out RendererPackedData packedData)
		{
		    packedData.params1 = UnpackBytes(params.x);
		    packedData.params2 = UnpackBytes(params.y);

		    packedData.rotation = UnpackRotation(AsUNorm(packedData.params1.x));
		    packedData.offsetMultiplier = AsSNorm(packedData.params1.z);
		    packedData.saturation = AsSNorm(packedData.params1.w);
		}

        half2 UnpackHalf2(const uint packedValue)
		{
		    return half2(f16tof32(packedValue), f16tof32(packedValue >> 16));
		}

        half4 UnpackHalf4(const uint2 packedValue)
		{
		    return half4(UnpackHalf2(packedValue.x), UnpackHalf2(packedValue.y));
		}

        void GetVertexData(const uint vertexId, out float4 positionCs, out float2 centeredUv, out RendererPackedData packedData)
		{
		    const uint quadVertexId = vertexId % 4;
		    const uint instanceId = asuint(_Indices[vertexId / 4]);

		    half2 positionOs;
		    positionOs.x = quadVertexId % 3 == 0 ? -1 : 1;
		    positionOs.y = quadVertexId < 2 ? 1 : -1;
		    centeredUv = positionOs;

		    const float4 rawPackedData = _PackedData[instanceId];
		    const half4 positionHalfSize = UnpackHalf4(asuint(rawPackedData.xy));
		    UnpackParams(asuint(rawPackedData.zw), packedData);
		    
		    const half2 positionWs = positionOs * positionHalfSize.zw + positionHalfSize.xy;
		    const float2 screenUv = ComputeBlobShadowCoordsRaw(positionWs,
		        _ToonRP_BlobShadows_Offset * packedData.offsetMultiplier + _ToonRP_BlobShadows_Min_Size.xy,
		        _ToonRP_BlobShadows_Min_Size.zw);
		    positionCs = ScreenUvToHClip(screenUv);
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
                float3 centeredUvParams : TEXCOORD0; // xy = uv, z = saturation
            };

            v2f VS(const uint vertexId : SV_VertexID)
            {
                v2f OUT;
                
                RendererPackedData packedData;
                GetVertexData(vertexId, OUT.positionCs, OUT.centeredUvParams.xy, packedData);

                OUT.centeredUvParams.z = packedData.saturation * _Saturation;
                
                return OUT;
            }

			float4 PS(const v2f IN) : SV_TARGET
            {
                return saturate(1.0f - length(IN.centeredUvParams.xy)) * IN.centeredUvParams.z; 
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
                float4 params : PARAMS; // xy = halfSize, z = cornerRadius, w = saturation
                float2 centeredUv : TEXCOORD0;
            };

            v2f VS(const uint vertexId : SV_VertexID)
            {
                v2f OUT;
                
                RendererPackedData packedData;
                float2 centeredUv;
                GetVertexData(vertexId, OUT.positionCs, centeredUv, packedData);

                float4 params;
                params.xy = AsUNorm(packedData.params2.xy);
                params.z = AsUNorm(packedData.params2.z);
                params.w = packedData.saturation * _Saturation;
                OUT.params = params;
                
                OUT.centeredUv = Rotate(centeredUv, packedData.rotation);

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
                return saturate(-SdfRoundBox(IN.centeredUv, halfSize, cornerRadius)) * IN.params.w; 
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
                float3 uvParams : TEXCOORD0; // xy = uv, z = saturation
            };

            v2f VS(const uint vertexId : SV_VertexID)
            {
                v2f OUT;
                
                float2 centeredUv;
                RendererPackedData packedData;
                GetVertexData(vertexId, OUT.positionCs, centeredUv, packedData);

                centeredUv = Rotate(centeredUv, packedData.rotation);

                const uint textureIndex = packedData.params2.x;
                float4 tilingOffset = _ToonRP_BlobShadows_BakedTexturesAtlas_TilingOffsets[textureIndex];
                OUT.uvParams.xy = (centeredUv * 0.5f + 0.5f) * tilingOffset.xy + tilingOffset.zw;

                OUT.uvParams.z = packedData.saturation * _Saturation;

                return OUT;
            }

			float4 PS(const v2f IN) : SV_TARGET
            {
                return SAMPLE_TEXTURE2D(_ToonRP_BlobShadows_BakedTexturesAtlas, sampler_ToonRP_BlobShadows_BakedTexturesAtlas, IN.uvParams.xy) * IN.uvParams.z;
            }

			ENDHLSL
		}
	}
}