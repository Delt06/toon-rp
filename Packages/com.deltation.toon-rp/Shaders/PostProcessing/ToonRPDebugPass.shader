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
	    
	    ENDHLSL
	    Pass
		{
		    Name "Toon RP Debug Pass: Tiled Lighting"

			HLSLPROGRAM

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

			float4 PS(const v2f IN) : SV_TARGET
            {
                const uint2 tileIndex = TiledLighting_ScreenPositionToTileIndex(IN.positionCs.xy);
                const uint flatTileIndex = TiledLighting_GetFlatTileIndex(tileIndex.x, tileIndex.y);

                float3 output = 0.0f;

                {
                    const uint lightGridIndex = TiledLighting_GetOpaqueLightGridIndex(flatTileIndex);
                    const uint startOffset = _TiledLighting_LightGrid[lightGridIndex].x;
                    const uint lightCount = _TiledLighting_LightGrid[lightGridIndex].y;

                    output.r = (float) lightCount / _AdditionalLightCount;
                }

                // {
                //     const uint lightGridIndex = TiledLighting_GetTransparentLightGridIndex(flatTileIndex);
                //     const uint startOffset = _TiledLighting_LightGrid[lightGridIndex].x;
                //     const uint lightCount = _TiledLighting_LightGrid[lightGridIndex].y;
                //
                //     output.g = (float) lightCount / 2;
                // }
                
                return float4(lerp(SampleSource(IN.uv), output, 0.5), 1.0f);
            }

			ENDHLSL
		}
	}
}