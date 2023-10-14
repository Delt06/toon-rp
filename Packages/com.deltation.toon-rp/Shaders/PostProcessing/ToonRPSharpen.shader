Shader "Hidden/Toon RP/Sharpen"
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
	    SAMPLER(sampler_MainTex);

	    float3 SampleSource(const float2 uv)
        {
            return SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, 0).rgb;
        }

	    float3 SampleSourceOffset(const float2 uv, const float2 offset)
        {
            return SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv + _MainTex_TexelSize.xy * offset, 0).rgb;
        }

	    //#pragma enable_d3d11_debug_symbols

	    #pragma vertex VS
		#pragma fragment PS

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
			HLSLPROGRAM

			float _Amount;

			// https://lettier.github.io/3d-game-shaders-for-beginners/sharpen.html
			float3 ApplySharpen(const float2 uv)
            {
                const float neighbor = _Amount * -1;
                const float center = _Amount * 4 + 1;

                float3 color =
                    SampleSourceOffset(uv, float2(0, 1)) * neighbor +
                    SampleSourceOffset(uv, float2(-1, 0)) * neighbor +
                    SampleSource(uv) * center +
                    SampleSourceOffset(uv, float2(1, 0)) * neighbor +
                    SampleSourceOffset(uv, float2(0, -1)) * neighbor;
                return color;
            }

			float4 PS(const v2f IN) : SV_TARGET
            {
                float3 color = ApplySharpen(IN.uv);
                return float4(color, 1.0f);
            }

			ENDHLSL
		}
    }
}