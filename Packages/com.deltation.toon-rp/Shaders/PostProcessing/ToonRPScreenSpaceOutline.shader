Shader "Hidden/Toon RP/Outline (Screen Space)"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	    _OutlineColor ("Outline Color", Color) = (0, 0, 0, 0)
	    _ColorThreshold ("Color Threshold", Float) = 0
	    _DepthThreshold ("Depth Threshold", Float) = 0
	    _NormalsThreshold ("Normals Threshold", Float) = 0
	}
	SubShader
	{
		Pass
		{
		    Name "Toon RP Outline (Screen Space)"

			HLSLPROGRAM

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            #include "../../ShaderLibrary/Common.hlsl"
            #include "../../ShaderLibrary/DepthNormals.hlsl"
            #include "../../ShaderLibrary/Fog.hlsl"
            #include "../../ShaderLibrary/Textures.hlsl"

			#pragma enable_d3d11_debug_symbols

			#pragma multi_compile_fog

			#pragma multi_compile_local_fragment _ _COLOR
			#pragma multi_compile_local_fragment _ _DEPTH
			#pragma multi_compile_local_fragment _ _NORMALS
			#pragma multi_compile_local_fragment _ _USE_FOG

	        #pragma vertex VS
	        #pragma fragment PS

			TEXTURE2D(_MainTex);            

            #define POINT_SAMPLER sampler_point_clamp
            SAMPLER(POINT_SAMPLER);

			CBUFFER_START(UnityPerMaterial)
			float3 _OutlineColor;
			float _ColorThreshold;
			float _DepthThreshold;
			float _NormalsThreshold;
			DECLARE_TEXEL_SIZE(_MainTex);
			CBUFFER_END

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

			struct Kernel3
            {
                float3 values[9];
            };

			struct Kernel
            {
                float3 values[9];
            };

			#define FILL_KERNEL(kernel, funcName, du, dv) \
			    kernel.values[0] = funcName(uv + float2(-du, -dv)); \
			    kernel.values[1] = funcName(uv + float2(0, -dv)); \
			    kernel.values[2] = funcName(uv + float2(du, -dv)); \
			    kernel.values[3] = funcName(uv + float2(-du, 0)); \
			    kernel.values[4] = funcName(uv); \
			    kernel.values[5] = funcName(uv + float2(du, 0)); \
			    kernel.values[6] = funcName(uv + float2(-du, dv)); \
			    kernel.values[7] = funcName(uv + float2(0.0, dv)); \
			    kernel.values[8] = funcName(uv + float2(du, dv)); \

			#define SOBEL_EDGE_H(kernel) ((kernel.values[2] + 2 * kernel.values[5] + kernel.values[8] - (kernel.values[0] + 2 * kernel.values[3] + kernel.values[6])))
			#define SOBEL_EDGE_V(kernel) ((kernel.values[0] + 2 * kernel.values[1] + kernel.values[2] - (kernel.values[6] + 2 * kernel.values[7] + kernel.values[8])))

			float3 SampleSceneColor(const float2 uv)
			{
			    return SAMPLE_TEXTURE2D_LOD(_MainTex, POINT_SAMPLER, uv, 0);
			}

			float SampleLinearDepth(const float2 uv)
			{
			    const float depth = SampleDepthTexture(uv);
			    return LinearEyeDepth(depth, _ZBufferParams);
			}

			Kernel3 SampleColorKernel(const float2 uv)
			{
			    const float du = _MainTex_TexelSize.x;
			    const float dv = _MainTex_TexelSize.y;

			    Kernel3 kernel;
			    FILL_KERNEL(kernel, SampleSceneColor, du, dv);

			    return kernel;
			}

			Kernel3 SampleNormalsKernel(const float2 uv)
			{
			    const float du = _MainTex_TexelSize.x;
			    const float dv = _MainTex_TexelSize.y;

			    Kernel3 kernel;
			    FILL_KERNEL(kernel, SampleNormalsTexture, du, dv);
			    return kernel;
			}

			Kernel SampleDepthKernel(const float2 uv)
			{
			    const float du = _MainTex_TexelSize.x;
			    const float dv = _MainTex_TexelSize.y;

			    Kernel kernel;
			    FILL_KERNEL(kernel, SampleLinearDepth, du, dv);
			    return kernel;
			}

			float ComputeSobelStrength3(const in Kernel3 kernel, const float threshold)
			{
			    const float3 sobelEdgeH = SOBEL_EDGE_H(kernel);
			    const float3 sobelEdgeV = SOBEL_EDGE_V(kernel);
                const float3 sobel = sqrt(sobelEdgeH * sobelEdgeH + sobelEdgeV * sobelEdgeV);

			    const float sobelTotal = sobel.x + sobel.y + sobel.z;
			    return step(threshold, sobelTotal);
			}

			float ComputeSobelStrength(const in Kernel kernel, const float threshold)
			{
			    const float sobelEdgeH = SOBEL_EDGE_H(kernel);
			    const float sobelEdgeV = SOBEL_EDGE_V(kernel);
                const float sobel = length(float2(sobelEdgeH, sobelEdgeV));
			    
			    return step(threshold, sobel);
			}

			float4 PS(const v2f IN) : SV_TARGET
			{
                float sobelStrength = 0.0;
			    const float2 uv = IN.uv;
			    float3 sceneColor;
			    float sceneDepth;

                {
                    #ifdef _COLOR
			        const Kernel3 colorKernel = SampleColorKernel(uv);
			        sceneColor = colorKernel.values[4];
			        sobelStrength = max(sobelStrength, ComputeSobelStrength3(colorKernel, _ColorThreshold));
			        #else // !_COLOR
			        sceneColor = float4(SampleSceneColor(uv), 1);
			        #endif // _COLOR
                }

                {
                    #ifdef _NORMALS
                    const Kernel3 normalKernel = SampleNormalsKernel(uv);
			        sobelStrength += max(sobelStrength, ComputeSobelStrength3(normalKernel, _NormalsThreshold));
                    #endif // _NORMALS
                }

                {
                    #ifdef _DEPTH
                    const Kernel depthKernel = SampleDepthKernel(uv);
                    sceneDepth = depthKernel.values[4];
                    sobelStrength += max(sobelStrength, ComputeSobelStrength(depthKernel, _DepthThreshold));
                    #else // !_DEPTH
                    sceneDepth = SampleLinearDepth(uv);
                    #endif // _DEPTH
                }

			    float3 outlineColor = _OutlineColor;
			    #if defined(FOG_ANY) && defined(_USE_FOG)
			    const float fogFactor = ComputeFogFactorZ0ToFar(sceneDepth);
			    outlineColor = MixFog(outlineColor, fogFactor);
			    #endif // FOG_ANY && _USE_FOG

			    const float3 finalColor = lerp(sceneColor, outlineColor, saturate(sobelStrength));
			    return float4(finalColor, 1);
			}

			ENDHLSL
		}
	}
}