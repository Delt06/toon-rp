Shader "Hidden/Toon RP/Debug Pass"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
	    HLSLINCLUDE

	    #include "../../ShaderLibrary/Common.hlsl"
	    #include "../../ShaderLibrary/Textures.hlsl"

	    TEXTURE2D(_MainTex);
        DECLARE_TEXEL_SIZE(_MainTex);

        #define LINEAR_SAMPLER sampler_linear_clamp
        SAMPLER(LINEAR_SAMPLER);

	    float3 SampleSource(const float2 uv)
        {
            return SAMPLE_TEXTURE2D_LOD(_MainTex, LINEAR_SAMPLER, uv, 0).rgb;
        }

	    //#pragma enable_d3d11_debug_symbols

	    #pragma vertex VS
		#pragma fragment PS
		
		#include "../../ShaderLibrary/TiledLighting.hlsl"

		struct appdata
        {
            float3 position : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            float4 positionCs : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        v2f VS(const appdata IN)
        {
            v2f OUT;
            OUT.uv = IN.uv;
            OUT.positionCs = TransformObjectToHClip(IN.position);
            return OUT;
        }

	    float4 PS(v2f IN);
	    
	    ENDHLSL
	    Pass
		{
		    Name "Toon RP Debug Pass: Tiled Lighting"

			HLSLPROGRAM

			bool _TiledLighting_ShowOpaque;
			bool _TiledLighting_ShowTransparent;

			float4 PS(const v2f IN) : SV_TARGET
            {
                const uint2 tileIndex = TiledLighting_ScreenPositionToTileIndex(IN.positionCs.xy);
                const uint flatTileIndex = TiledLighting_GetFlatTileIndex(tileIndex.x, tileIndex.y);

                float3 output = 0.0f;

                if (_AdditionalLightCount)
                {
                    if (_TiledLighting_ShowOpaque)
                    {
                        const uint lightGridIndex = TiledLighting_GetOpaqueLightGridIndex(flatTileIndex);
                        const uint lightCount = _TiledLighting_LightGrid[lightGridIndex].y;
                    
                        output.r = (float) lightCount / _TiledLighting_ReservedLightsPerTile;

                        if (output.r > 0.0f)
                        {
                            output.rg = lerp(float2(0.0f, 1.0f), float2(1.0f, 0.0f), output.r);
                        }
                    }

                    if (_TiledLighting_ShowTransparent)
                    {
                        const uint lightGridIndex = TiledLighting_GetTransparentLightGridIndex(flatTileIndex);
                        const uint lightCount = _TiledLighting_LightGrid[lightGridIndex].y;
                    
                        output.b = (float) lightCount / _TiledLighting_ReservedLightsPerTile;

                        if (output.b > 0.0f)
                        {
                            output.b = output.g * 0.75f + 0.25f;
                        }
                    }
                }
                
                return float4(lerp(SampleSource(IN.uv), output, 0.75f), 1.0f);
            }

			ENDHLSL
		}

        Pass
		{
		    Name "Toon RP Debug Pass: Depth"

			HLSLPROGRAM

			#include "../../ShaderLibrary/DepthNormals.hlsl"

			float4 PS(const v2f IN) : SV_TARGET
            {
                const float depth = SampleDepthTexture(IN.uv);
                return float4(depth, 0.0f, 0.0f, 1.0f);
            }

			ENDHLSL
		}

        Pass
		{
		    Name "Toon RP Debug Pass: Normals"

			HLSLPROGRAM

			#include "../../ShaderLibrary/DepthNormals.hlsl"

			float4 PS(const v2f IN) : SV_TARGET
            {
                const float3 normals = SampleNormalsTexture(IN.uv) * 0.5f + 0.5f;
                return float4(normals, 1.0f);
            }

			ENDHLSL
		}

        Pass
		{
		    Name "Toon RP Debug Pass: Motion Vectors"

			HLSLPROGRAM

			#include "../../ShaderLibrary/MotionVectors.hlsl"

			float _MotionVectors_Scale;
			float _MotionVectors_SceneIntensity;

			float4 PS(const v2f IN) : SV_TARGET
            {
                const float2 motionVectorsSample = SAMPLE_TEXTURE2D_LOD(_ToonRP_MotionVectorsTexture, sampler_ToonRP_MotionVectorsTexture, IN.uv, 0).rg;
                const float3 result = SampleSource(IN.uv) * _MotionVectors_SceneIntensity + float3(abs(motionVectorsSample) * _MotionVectors_Scale, 0.0f);
                return float4(result, 1.0f);
            }

			ENDHLSL
		}
	}
}