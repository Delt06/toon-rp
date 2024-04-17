Shader "Hidden/Toon RP/Debug Pass"
{
	Properties
	{
	}
	SubShader
	{
	    Cull Off ZWrite Off ZTest Always
	    
	    HLSLINCLUDE

	    #include "Packages/com.deltation.toon-rp/ShaderLibrary/Common.hlsl"
	    #include "Packages/com.deltation.toon-rp/ShaderLibrary/Textures.hlsl"

	    TEXTURE2D(_MainTex);
        DECLARE_TEXEL_SIZE(_MainTex);

        #define LINEAR_SAMPLER sampler_linear_clamp
        SAMPLER(LINEAR_SAMPLER);

	    float3 SampleSource(const float2 uv)
        {
            return SAMPLE_TEXTURE2D_X_LOD(_MainTex, LINEAR_SAMPLER, uv, 0).rgb;
        }

	    //#pragma enable_d3d11_debug_symbols

	    #pragma vertex Vert
		#pragma fragment Frag
		
		#include "Packages/com.deltation.toon-rp/ShaderLibrary/TiledLighting.hlsl"
	    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

	    float4 Frag(Varyings IN);
	    
	    ENDHLSL
	    Pass
		{
		    Name "Toon RP Debug Pass: Tiled Lighting"

			HLSLPROGRAM

			float4 Frag(const Varyings IN) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                const float2 uv = UnityStereoTransformScreenSpaceTex(IN.texcoord);
                
                const uint2 tileIndex = TiledLighting_ScreenPositionToTileIndex(IN.positionCS.xy);
                const uint flatTileIndex = TiledLighting_GetFlatTileIndex(tileIndex.x, tileIndex.y);

                float3 output = 0.0f;

                if (_AdditionalLightCount)
                {
                    const uint lightGridIndex = TiledLighting_GetOpaqueLightGridIndex(flatTileIndex);
                    const uint lightCount = _TiledLighting_LightGrid[lightGridIndex].y;
                
                    output.r = (float) lightCount / _TiledLighting_ReservedLightsPerTile;

                    const float3 lowColor = float3(0.0f, 0.0f, 1.0f);
                    const float3 midColor = float3(0.0f, 1.0f, 0.0f);
                    const float3 highColor = float3(1.0f, 0.0f, 0.0f);
                    
                    if (output.r > 0.5f)
                    {
                        output.rgb = lerp(midColor, highColor, output.r * 2.0f - 1.0f);
                    }
                    else if (output.r > 0.0f)
                    {
                        output.rgb = lerp(lowColor, midColor, output.r * 2.0f);
                    }
                }
                
                return float4(lerp(SampleSource(uv), output, 0.75f), 1.0f);
            }

			ENDHLSL
		}

        Pass
		{
		    Name "Toon RP Debug Pass: Depth"

			HLSLPROGRAM

			#include "../../ShaderLibrary/DepthNormals.hlsl"

			float4 Frag(const Varyings IN) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                const float2 uv = UnityStereoTransformScreenSpaceTex(IN.texcoord);
                const float depth = SampleDepthTexture(uv);
                return float4(depth, 0.0f, 0.0f, 1.0f);
            }

			ENDHLSL
		}

        Pass
		{
		    Name "Toon RP Debug Pass: Normals"

			HLSLPROGRAM

			#include "../../ShaderLibrary/DepthNormals.hlsl"

			float4 Frag(const Varyings IN) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                const float2 uv = UnityStereoTransformScreenSpaceTex(IN.texcoord);
                const float3 normals = SampleNormalsTexture(uv) * 0.5f + 0.5f;
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

			float4 Frag(const Varyings IN) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                const float2 uv = UnityStereoTransformScreenSpaceTex(IN.texcoord);
                const float2 motionVectorsSample = SampleMotionVectors(uv);
                const float3 result = SampleSource(uv) * _MotionVectors_SceneIntensity + float3(abs(motionVectorsSample) * _MotionVectors_Scale, 0.0f);
                return float4(result, 1.0f);
            }

			ENDHLSL
		}
	}
}