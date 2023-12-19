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
            half invSqrRange;
        };

        FakeLightData UnpackFakeLightData(const float4 packedValue)
        {
            half4 params1 = UnpackHalf4(asuint(packedValue.xy));
            half4 params2 = UnpackHalf4(asuint(packedValue.zw));

            FakeLightData fakeLightData;
            fakeLightData.center = params1.xyz;
            fakeLightData.range = params1.w;
            fakeLightData.color = params2.xyz;
            fakeLightData.invSqrRange = params2.w;
            return fakeLightData;
        }

        struct InverpolatedParams
        {
            half2 positionWsXz;
            half3 color;
            half invSqrRange;
            half3 center;
        };

        void GetVertexData(const uint vertexId, out float4 positionCs, out InverpolatedParams inverpolatedParams)
		{
		    const uint quadVertexId = vertexId % 4;
            const uint instanceId = vertexId / 4;

		    half2 positionOs;
		    positionOs.x = quadVertexId % 3 == 0 ? -1 : 1;
		    positionOs.y = quadVertexId < 2 ? 1 : -1;

		    const float4 rawPackedData = _FakeAdditionalLights[instanceId];
            FakeLightData fakeLightData = UnpackFakeLightData(rawPackedData);

            const half2 positionWs = positionOs * fakeLightData.range + fakeLightData.center.xz;
		    const half2 screenUv = FakeAdditionalLights_PositionToUV(positionWs);
		    positionCs = ScreenUvToHClip(screenUv);

            inverpolatedParams.positionWsXz = positionWs;
            inverpolatedParams.center = fakeLightData.center;
            inverpolatedParams.color = fakeLightData.color;
            inverpolatedParams.invSqrRange = fakeLightData.invSqrRange;
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
            
                half2 positionWsXz : POSITION_WS;
                half3 color : COLOR;
                half invSqrRange : INV_SQR_RANGE;
                half3 center : CENTER_XZ;
            };

            v2f VS(const uint vertexId : SV_VertexID)
            {
                v2f OUT;
                
                InverpolatedParams inverpolatedParams;
                GetVertexData(vertexId, OUT.positionCs, inverpolatedParams);

                OUT.positionWsXz = inverpolatedParams.positionWsXz;
                OUT.color = inverpolatedParams.color;
                OUT.invSqrRange = inverpolatedParams.invSqrRange;
                OUT.center = inverpolatedParams.center;
                
                return OUT;
            }

			half4 PS(const v2f IN) : SV_TARGET
            {
                half3 receiverPosition;
                receiverPosition.xz = IN.positionWsXz;
                receiverPosition.y = _ReceiverPlaneY;
                
                const half3 offset = IN.center - receiverPosition;
                const half distanceSqr = max(dot(offset, offset), 0.00001);
                half distanceAttenuation = Sq(
                    saturate(1.0f - Sq(distanceSqr * IN.invSqrRange))
                );
                distanceAttenuation = distanceAttenuation / distanceSqr;
                distanceAttenuation = distanceAttenuation * _AdditionalLightRampOffset.z;
                distanceAttenuation += _AdditionalLightRampOffset.x;
                distanceAttenuation = saturate(distanceAttenuation);

                const half3 color = IN.color * distanceAttenuation;
                return half4(color, distanceAttenuation);
            }

			ENDHLSL
		}
	}
}