Shader "Toon RP/Decal"
{
	Properties
	{
		[MainColor] [HDR] 
		_MainColor ("Color", Color) = (1, 1, 1, 1)
		[MainTexture]
		_MainTexture ("Texture", 2D) = "white" {}
	    _AngleDiscardThreshold ("Angle Discard Threshold", Range(0, 1)) = 0.5
	    _DepthBias ("Depth Bias", Range(-1, 1)) = 0
	}
	SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "ToonRP" }
		Blend SrcAlpha OneMinusSrcAlpha
		LOD 100
		ZWrite Off
	    
	    HLSLINCLUDE

	    //#pragma enable_d3d11_debug_symbols

	    #pragma vertex VS
		#pragma fragment PS
	    
	    ENDHLSL

		Pass
		{
		    Name "Toon RP Decal"
			Tags{ "LightMode" = "ToonRPForward" }
			
			HLSLPROGRAM

			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#include "Packages/com.deltation.toon-rp/ShaderLibrary/Common.hlsl"
			#include "Packages/com.deltation.toon-rp/ShaderLibrary/DepthNormals.hlsl"
			#include "Packages/com.deltation.toon-rp/ShaderLibrary/Fog.hlsl"
			#include "Packages/com.deltation.toon-rp/ShaderLibrary/Textures.hlsl"

			TEXTURE2D(_MainTexture);
			SAMPLER(sampler_MainTexture);

			CBUFFER_START(UnityPerMaterial)
			    half4 _MainColor;
			    DECLARE_TILING_OFFSET(_MainTexture);
			    half _AngleDiscardThreshold;
			    float _DepthBias;
			CBUFFER_END
			
			struct appdata
            {
                float3 vertex : POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

			struct v2f
			{
			    float4 positionCs : SV_POSITION;
			    float2 uv : TEXCOORD0;

			    TOON_RP_FOG_FACTOR_INTERPOLANT
			};

			void ApplyDepthBias(inout float4 positionCs)
			{
#if UNITY_REVERSED_Z
			    positionCs.z -= _DepthBias;
#else // !UNITY_REVERSED_Z
			    positionCs.z += _DepthBias;
#endif // UNITY_REVERSED_Z
			}

            v2f VS(const appdata IN)
            {
                v2f OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                const float2 uv = APPLY_TILING_OFFSET(IN.uv, _MainTexture);
                OUT.uv = uv;

                const float3 positionWs = TransformObjectToWorld(IN.vertex);
                float4 positionCs = TransformWorldToHClip(positionWs);
			    ApplyDepthBias(positionCs);
                
                OUT.positionCs = positionCs;

                TOON_RP_FOG_FACTOR_TRANSFER(OUT, positionCs);

                return OUT;
            }

            float4 PS(const v2f IN) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                const float2 screenUv = UnityStereoTransformScreenSpaceTex(PositionHClipToScreenUv(IN.positionCs));
                const float ndcDepth = RawToNdcDepth(SampleDepthTexture(screenUv));

                const float3 reconstructedPositionWs = ComputeWorldSpacePosition(screenUv, ndcDepth, UNITY_MATRIX_I_VP);
                const half3 reconstructedPositionOs = TransformWorldToObject(reconstructedPositionWs);

                const half3 sceneNormalOs = TransformWorldToObjectNormal(SampleNormalsTexture(screenUv));
                const half clipAngle = step(_AngleDiscardThreshold, sceneNormalOs.z);
                clip(0.5h - reconstructedPositionOs - clipAngle);

                const float2 decalUv = reconstructedPositionOs.xy + 0.5h; // [-0.5, 0.5] -> [0.0, 1.0]
                float4 albedo = _MainColor * SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, decalUv);
                
                float3 outputColor = albedo.rgb;
                TOON_RP_FOG_MIX(IN, outputColor);
                
                return float4(outputColor, albedo.a);
            }
			
			ENDHLSL
		}
	}
}