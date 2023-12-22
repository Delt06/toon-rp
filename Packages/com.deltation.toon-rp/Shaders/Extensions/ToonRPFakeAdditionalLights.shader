Shader "Hidden/Toon RP/Fake Additional Lights"
{
	Properties
	{
    }
	SubShader
	{
	    Cull Off
    
        Blend One One 
        
        HLSLINCLUDE

        //#pragma enable_d3d11_debug_symbols
		
        #include "../../ShaderLibrary/Common.hlsl"
        #include "../../ShaderLibrary/FakeAdditionalLights.hlsl"
        #include "../../ShaderLibrary/Lighting.hlsl"

        #define BATCH_SIZE 256

        #define LIGHT_TYPE_SPOT 0

        CBUFFER_START(_ToonRP_FakeAdditionalLights_PackedData)
        float4 _FakeAdditionalLights[BATCH_SIZE];
        CBUFFER_END

        half4 ScreenUvToHClip(const half2 screenUv)
		{
		    half4 positionCs = float4(screenUv * 2.0f - 1.0f, 0.5f, 1.0f);

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

        half AsUNormHalf(const uint packedByte)
		{
		    return saturate(packedByte / 255.0f); 
		}

        float AsSNorm(const uint packedByte)
		{
		    return AsUNorm(packedByte) * 2.0f - 1.0f; 
		}

        half AsSNormHalf(const uint packedByte)
		{
		    return AsUNormHalf(packedByte) * 2.0f - 1.0f; 
		}

        float2 AsUNorm(const uint2 packedByte)
		{
		    return float2(AsUNorm(packedByte.x), AsUNorm(packedByte.y)); 
		}

        half2 UnpackHalf2(const uint packedValue)
		{
		    return half2(f16tof32(packedValue), f16tof32(packedValue >> 16));
		}

        half4 UnpackHalf4(const uint2 packedValue)
		{
		    return half4(UnpackHalf2(packedValue.x), UnpackHalf2(packedValue.y));
		}

        struct FakeLightData
        {
            half3 center;
            half range;
            half3 color;
            half3 direction;
            half spotAngleCos;
            half invSqrRange;
            half type;
        };

        FakeLightData UnpackFakeLightData(const float4 packedValue)
        {
            half4 bytes_00_07 = UnpackHalf4(asuint(packedValue.xy));
            uint4 bytes_08_11 = UnpackBytes(asuint(packedValue.z));
            uint4 bytes_12_15 = UnpackBytes(asuint(packedValue.w));

            FakeLightData fakeLightData;
            fakeLightData.center = bytes_00_07.xyz;
            fakeLightData.range = bytes_00_07.w;

            fakeLightData.color =
                half3(AsUNormHalf(bytes_08_11.x), AsUNormHalf(bytes_08_11.y), AsUNormHalf(bytes_08_11.z));
            fakeLightData.direction = normalize(
                half3(AsSNormHalf(bytes_08_11.w), AsSNormHalf(bytes_12_15.x), AsSNormHalf(bytes_12_15.y))
                );

            fakeLightData.spotAngleCos = AsUNormHalf(bytes_12_15.z);
            fakeLightData.type = bytes_12_15.w;
            
            fakeLightData.invSqrRange = 1.0f / max(fakeLightData.range * fakeLightData.range, 0.00001f);
            return fakeLightData;
        }

        void GetVertexData(const uint vertexId, out float4 positionCs, out half2 positionWs, out uint instanceId)
		{
		    const uint quadVertexId = vertexId % 4;
            instanceId = vertexId / 4;

		    half2 positionOs;
		    positionOs.x = quadVertexId % 3 == 0 ? -1 : 1;
		    positionOs.y = quadVertexId < 2 ? 1 : -1;

		    const float4 rawPackedData = _FakeAdditionalLights[instanceId];
            FakeLightData fakeLightData = UnpackFakeLightData(rawPackedData);

            const half distanceFromGround = abs(fakeLightData.center.y - _ToonRP_FakeAdditionalLights_ReceiverPlaneY);
            const half sectionRadius = sqrt(fakeLightData.range * fakeLightData.range - distanceFromGround * distanceFromGround);
            positionWs = positionOs * sectionRadius + fakeLightData.center.xz;
		    const half2 screenUv = FakeAdditionalLights_PositionToUV(positionWs);
		    positionCs = ScreenUvToHClip(screenUv);
        }
        
        ENDHLSL
            
        Pass
		{
		    Name "Fake Additional Light"
		    
			HLSLPROGRAM

            #pragma vertex VS
		    #pragma fragment PS

            struct v2f
		    {
                half4 positionCs : SV_POSITION;
                half2 positionWs : POSITION_WS;
                nointerpolation uint instanceId : INSTANCE_ID;
            };

            v2f VS(const uint vertexId : SV_VertexID)
            {
                v2f OUT;
                
                GetVertexData(vertexId, OUT.positionCs, OUT.positionWs, OUT.instanceId);
                
                return OUT;
            }

            half ComputeDistanceAttenuation(
                const half3 offsetTowardsLight,
                const half invSqrRange
                )
            {
                const half distanceSqr = max(dot(offsetTowardsLight, offsetTowardsLight), 0.00001);
                half distanceAttenuation = Sq(
                    saturate(1.0f - Sq(distanceSqr * invSqrRange))
                );
                distanceAttenuation = distanceAttenuation / distanceSqr;
                return distanceAttenuation;
            }

            half ComputeSpotLightConeAttenuation(
                const half3 directionTowardsLight,
                const half3 lightDirection,
                const half angleCos
                )
            {
                const float maxCos = (angleCos + 1.0f) / 2.0f;
                const float cosAngle = dot(lightDirection, -directionTowardsLight);
                return smoothstep(angleCos, maxCos, cosAngle);
            }

			half4 PS(const v2f IN) : SV_TARGET
            {
                half3 receiverPosition;
                receiverPosition.xz = IN.positionWs;
                receiverPosition.y = _ToonRP_FakeAdditionalLights_ReceiverPlaneY;

                const float4 rawPackedData = _FakeAdditionalLights[IN.instanceId];
                const FakeLightData fakeLightData = UnpackFakeLightData(rawPackedData);
                
                const half3 offset = fakeLightData.center - receiverPosition;

                
                half attenuation = ComputeDistanceAttenuation(offset, fakeLightData.invSqrRange);

                if (fakeLightData.type == LIGHT_TYPE_SPOT)
                {
                    attenuation *= ComputeSpotLightConeAttenuation(normalize(offset), fakeLightData.direction, fakeLightData.spotAngleCos);
                }
                
                attenuation = attenuation * _AdditionalLightRampOffset.z;
                attenuation += _AdditionalLightRampOffset.x;
                attenuation = saturate(attenuation);

                const float ramp = ComputeRamp(attenuation, _ToonRP_FakeAdditionalLights_Ramp);
                const half3 color = fakeLightData.color * ramp;
                return half4(color, attenuation);
            }

			ENDHLSL
		}
	}
}