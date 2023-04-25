Shader "Hidden/Toon RP/FXAA"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
	    // https://catlikecoding.com/unity/tutorials/advanced-rendering/fxaa/
		Pass
		{
		    Name "Toon RP FXAA"
			
			HLSLPROGRAM

			#pragma enable_d3d11_debug_symbols

	        #pragma vertex VS
		    #pragma fragment PS

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            #include "../../ShaderLibrary/Common.hlsl"
            #include "../../ShaderLibrary/Textures.hlsl"

            TEXTURE2D(_MainTex);
            DECLARE_TEXEL_SIZE(_MainTex);

            float _FixedContrastThreshold;
            float _RelativeContrastThreshold;
            float _SubpixelBlendingFactor;

            #define LINEAR_SAMPLER sampler_linear_clamp
            SAMPLER(LINEAR_SAMPLER);

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

			#define EDGE_SEARCH_STEPS 3
            #define EDGE_SEARCH_STEP_SIZES 1.5, 2.0, 2.0
            #define EDGE_SEARCH_LAST_STEP_GUESS 8.0

            static const float EdgeStepSizes[EDGE_SEARCH_STEPS] = { EDGE_SEARCH_STEP_SIZES };

			float3 SampleSceneColor(const float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, LINEAR_SAMPLER, uv);
            }

            float GetSceneLuminance(float2 uv, float uOffset = 0.0, float vOffset = 0.0)
            {
                uv += float2(uOffset, vOffset) * _MainTex_TexelSize.xy;
                return sqrt(Luminance(SampleSceneColor(uv).rgb));
            }

			struct LuminanceNeighborhood
            {
                float center;
                float north;
                float east;
                float south;
                float west;

                float northEast;
                float southEast;
                float northWest;
                float southWest;


                float highest;
                float lowest;
                float range;
            };

			LuminanceNeighborhood GetLuminanceNeighborhood(float2 uv)
            {
                LuminanceNeighborhood luminance;

                luminance.center = GetSceneLuminance(uv);
                luminance.north = GetSceneLuminance(uv, 0, 1);
                luminance.east = GetSceneLuminance(uv, 1, 0);
                luminance.south = GetSceneLuminance(uv, 0, -1);
                luminance.west = GetSceneLuminance(uv, -1, 0);

                luminance.northEast = GetSceneLuminance(uv, 1, 1);
                luminance.southEast = GetSceneLuminance(uv, 1, -1);
                luminance.northWest = GetSceneLuminance(uv, -1, 1);
                luminance.southWest = GetSceneLuminance(uv, -1, -1);

                luminance.highest = max(luminance.center, max(luminance.north, max(luminance.east, max(luminance.south, luminance.west))));
                luminance.lowest = min(luminance.center, min(luminance.north, min(luminance.east, min(luminance.south, luminance.west))));
                luminance.range = luminance.highest - luminance.lowest;

                return luminance;
            }

			bool CanSkipFXAA(in LuminanceNeighborhood luminance)
            {
                return luminance.range < max(_FixedContrastThreshold, _RelativeContrastThreshold * luminance.highest);
            }

            float GetSubpixelBlendFactor(in LuminanceNeighborhood luminance)
            {
                float filter = 2.0 * (luminance.north + luminance.east + luminance.south + luminance.west);
                filter += luminance.northEast + luminance.northWest + luminance.southEast + luminance.southWest;
                filter *= 1.0f / 12.0f;
                filter = abs(filter - luminance.center);
                filter = saturate(filter / luminance.range);
                filter = smoothstep(0, 1, filter);
                return filter * filter * _SubpixelBlendingFactor;
            }

            bool IsHorizontalEdge(const LuminanceNeighborhood luminance)
            {
                const float horizontal =
                    2.0 * abs(luminance.north + luminance.south - 2.0 * luminance.center) +
                    abs(luminance.northEast + luminance.southEast - 2.0 * luminance.east) +
                    abs(luminance.northWest + luminance.southWest - 2.0 * luminance.west)
                    ;
                const float vertical =
                    2.0 * abs(luminance.east + luminance.west - 2.0 * luminance.center) +
                    abs(luminance.northEast + luminance.northWest - 2.0 * luminance.north) +
                    abs(luminance.southEast + luminance.southWest - 2.0 * luminance.south)
                    ;
                return horizontal >= vertical;
            }

            struct Edge
            {
                bool isHorizontal;
                float pixelStep;
                float luminanceGradient, otherLuminance;
            };

            Edge GetEdge(const in LuminanceNeighborhood luminance)
            {
                Edge edge;
                edge.isHorizontal = IsHorizontalEdge(luminance);

                float luminancePositive, luminanceNegative;
                if (edge.isHorizontal)
                {
                    edge.pixelStep = _MainTex_TexelSize.y;
                    luminancePositive = luminance.north;
                    luminanceNegative = luminance.south;
                }
                else
                {
                    edge.pixelStep = _MainTex_TexelSize.x;
                    luminancePositive = luminance.east;
                    luminanceNegative = luminance.west;
                }

                const float gradientPositive = abs(luminancePositive - luminance.center);
                const float gradientNegative = abs(luminanceNegative - luminance.center);

                if (gradientPositive < gradientNegative)
                {
                    edge.pixelStep = -edge.pixelStep;
                    edge.luminanceGradient = gradientNegative;
                    edge.otherLuminance = luminanceNegative;
                }
                else
                {
                    edge.luminanceGradient = gradientPositive;
                    edge.otherLuminance = luminancePositive;
                }

                return edge;
            }

            float GetEdgeBlendFactor(in LuminanceNeighborhood luminance, in Edge edge, float2 uv)
            {
                float2 edgeUV = uv;
                float2 uvStep = 0;

                if (edge.isHorizontal)
                {
                    edgeUV.y += 0.5 * edge.pixelStep;
                    uvStep.x = _MainTex_TexelSize.x;
                }
                else
                {
                    edgeUV.x += 0.5 * edge.pixelStep;
                    uvStep.y = _MainTex_TexelSize.y;
                }

                const float edgeLuminance = 0.5 * (luminance.center + edge.otherLuminance);
                const float gradientThreshold = 0.25 * edge.luminanceGradient;

                float2 uvPositive = edgeUV + uvStep;
                float lumaGradientPositive = GetSceneLuminance(uvPositive) - edgeLuminance;
                bool atEndPositive = abs(lumaGradientPositive) >= gradientThreshold;

                uint i;
                
                UNITY_UNROLL
                for (i = 0; i < EDGE_SEARCH_STEPS && !atEndPositive; ++i)
                {
                    uvPositive += uvStep * EdgeStepSizes[i];
                    lumaGradientPositive = GetSceneLuminance(uvPositive) - edgeLuminance;
                    atEndPositive = abs(lumaGradientPositive) >= gradientThreshold;
                }

                if (!atEndPositive)
                {
                    uvPositive += uvStep * EDGE_SEARCH_LAST_STEP_GUESS;
                }

                float2 uvNegative = edgeUV - uvStep;
                float lumaGradientNegative = GetSceneLuminance(uvNegative) - edgeLuminance;
                bool atEndNegative = abs(lumaGradientNegative) >= gradientThreshold;

                UNITY_UNROLL
                for (i = 0; i < EDGE_SEARCH_STEPS && !atEndNegative; ++i)
                {
                    uvNegative -= uvStep * EdgeStepSizes[i];
                    lumaGradientNegative = GetSceneLuminance(uvNegative) - edgeLuminance;
                    atEndNegative = abs(lumaGradientNegative) >= gradientThreshold;
                }

                if (!atEndNegative)
                {
                    uvNegative -= uvStep * EDGE_SEARCH_LAST_STEP_GUESS;
                }

                float distanceToEndPositive, distanceToEndNegative;
	            if (edge.isHorizontal)
                {
		            distanceToEndPositive = uvPositive.x - uv.x;
                    distanceToEndNegative = uv.x - uvNegative.x;
	            }
	            else
                {
		            distanceToEndPositive = uvPositive.y - uv.y;
                    distanceToEndNegative = uv.y - uvNegative.y;
	            }

                float distanceToNearestEnd;
                bool deltaSign;
                if (distanceToEndPositive <= distanceToEndNegative)
                {
                    distanceToNearestEnd = distanceToEndPositive;
                    deltaSign = lumaGradientPositive >= 0;
                }
                else
                {
                    distanceToNearestEnd = distanceToEndNegative;
                    deltaSign = lumaGradientNegative >= 0;
                }

                if (deltaSign == (luminance.center - edgeLuminance >= 0))
                {
                    return 0.0;
                }
                
                return 0.5 - distanceToNearestEnd / (distanceToEndPositive + distanceToEndNegative);
            }

			float4 PS(const v2f IN) : SV_TARGET
            {
                const float2 uv = IN.uv;
                const LuminanceNeighborhood luminance = GetLuminanceNeighborhood(uv);

                if (CanSkipFXAA(luminance))
                {
                    return float4(SampleSceneColor(uv), 1);
                }

                const Edge edge = GetEdge(luminance);
                const float blendFactor = max(
                    GetSubpixelBlendFactor(luminance),
                    GetEdgeBlendFactor(luminance, edge, uv)
                );
                float2 blendUv = uv;
                if (edge.isHorizontal)
                {
                    blendUv.y += blendFactor * edge.pixelStep;
                }
                else
                {
                    blendUv.x += blendFactor * edge.pixelStep;
                }

                return float4(SampleSceneColor(blendUv), 1);
            }

			ENDHLSL
		}
	}
}