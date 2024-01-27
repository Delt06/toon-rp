Shader "Hidden/Toon RP/Sharpen"
{
	Properties
	{
	}
	SubShader
	{
	    HLSLINCLUDE

	    #include "../../ShaderLibrary/Common.hlsl"
	    #include "../../ShaderLibrary/Textures.hlsl"

	    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

	    TEXTURE2D_X(_MainTex);
        DECLARE_TEXEL_SIZE(_MainTex);
	    SAMPLER(sampler_MainTex);

	    float3 SampleSource(const float2 uv)
        {
            return SAMPLE_TEXTURE2D_X_LOD(_MainTex, sampler_MainTex, uv, 0).rgb;
        }

	    float3 SampleSourceOffset(const float2 uv, const float2 offset)
        {
            return SAMPLE_TEXTURE2D_X_LOD(_MainTex, sampler_MainTex, uv + _MainTex_TexelSize.xy * offset, 0).rgb;
        }

	    //#pragma enable_d3d11_debug_symbols

	    #pragma vertex Vert
		#pragma fragment Frag

	    float4 Frag(Varyings IN);
	    
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

			float4 Frag(const Varyings IN) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                const float2 uv = UnityStereoTransformScreenSpaceTex(IN.texcoord);
                float3 color = ApplySharpen(uv);
                return float4(color, 1.0f);
            }

			ENDHLSL
		}
    }
}