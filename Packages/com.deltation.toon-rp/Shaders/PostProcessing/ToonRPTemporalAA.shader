Shader "Hidden/Toon RP/Temporal AA"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
	    HLSLINCLUDE

	    #include "../../ShaderLibrary/Common.hlsl"
	    #include "../../ShaderLibrary/MotionVectors.hlsl"
	    #include "../../ShaderLibrary/Textures.hlsl"

	    TEXTURE2D(_MainTex);
        DECLARE_TEXEL_SIZE(_MainTex);
	    SAMPLER(sampler_MainTex);

	    TEXTURE2D(_ToonRP_TAAHistory);
	    SAMPLER(sampler_ToonRP_TAAHistory);

	    float _ToonRP_TemporalAA_ModulationFactor;

	    float3 SampleSource(const float2 uv)
        {
            return SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, 0).rgb;
        }

	    float3 SampleSourceOffset(const float2 uv, const float2 offset)
        {
            return SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv + _MainTex_TexelSize.xy * offset, 0).rgb;
        }

	    float3 SampleHistory(const float2 uv)
	    {
	        return SAMPLE_TEXTURE2D_LOD(_ToonRP_TAAHistory, sampler_ToonRP_TAAHistory, uv, 0).rgb;
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

			float4 PS(const v2f IN) : SV_TARGET
            {
                // Originally from here: https://sugulee.wordpress.com/2021/06/21/temporal-anti-aliasingtaa-tutorial/
                // Adapted from the version from: https://github.com/Delt06/dx12-renderer/blob/master/Framework/shaders/TAA_Resolve_PS.hlsl
                const float2 uv = IN.uv;
                const float2 velocity = SampleMotionVectors(uv);
                const float2 previousPixelUv = uv - velocity;

                const float3 currentColor = SampleSource(uv);
                const float3 historyColor = SampleHistory(previousPixelUv);

                const float3 nearColor0 = SampleSourceOffset(uv, float2(1, 0));
                const float3 nearColor1 = SampleSourceOffset(uv, float2(0, 1));
                const float3 nearColor2 = SampleSourceOffset(uv, float2(-1, 0));
                const float3 nearColor3 = SampleSourceOffset(uv, float2(0, -1));

                const float3 boxMin = min(currentColor, min(nearColor0, min(nearColor1, min(nearColor2, nearColor3))));
                const float3 boxMax = max(currentColor, max(nearColor0, max(nearColor1, max(nearColor2, nearColor3))));

                const float3 boxedHistoryColor = clamp(historyColor, boxMin, boxMax);
                float3 resultColor = lerp(currentColor, boxedHistoryColor, _ToonRP_TemporalAA_ModulationFactor);
                return float4(resultColor, 1.0f);
            }

			ENDHLSL
		}
    }
}