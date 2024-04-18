Shader "Hidden/Toon RP/Motion Blur"
{
	Properties
	{
	}
	SubShader
	{
	    HLSLINCLUDE

	    #include "../../ShaderLibrary/Common.hlsl"
	    #include "../../ShaderLibrary/MotionVectors.hlsl"
	    #include "../../ShaderLibrary/Textures.hlsl"

	    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

	    TEXTURE2D_X(_MainTex);
        DECLARE_TEXEL_SIZE(_MainTex);
	    SAMPLER(sampler_MainTex);

	    float3 SampleSource(const float2 uv)
        {
            return SAMPLE_TEXTURE2D_X_LOD(_MainTex, sampler_MainTex, uv, 0).rgb;
        }

	    //#pragma enable_d3d11_debug_symbols

	    #pragma vertex Vert
		#pragma fragment Frag

	    float4 Frag(Varyings IN);
	    
	    ENDHLSL
	    Pass
		{
			HLSLPROGRAM

			float _ToonRP_MotionBlur_NumSamples;
			float _ToonRP_MotionBlur_WeightChangeRate;
			float _ToonRP_MotionBlur_Strength;
			float _ToonRP_MotionBlur_TargetFPS;

			float3 Apply(const float2 uv)
            {
			    const float velocityFactor = -_ToonRP_MotionBlur_Strength * _ToonRP_MotionBlur_TargetFPS * unity_DeltaTime.x;
                float2 velocity = SampleMotionVectors(uv) * velocityFactor;
			    const float numSamples = _ToonRP_MotionBlur_NumSamples;
			    const float invNumSamplesMinusOne = rcp(float(numSamples - 1));

			    float currentWeight = 1.0f;
			    float totalWeight = 0.0f;
                float3 color = 0.0f;

			    for (int i = 0; i < numSamples; ++i)
			    {
			        const float2 offset = velocity * (float(i) * invNumSamplesMinusOne);
                    const float2 uvWithOffset = uv + offset;
			        color += SampleSource(uvWithOffset) * currentWeight;
			        totalWeight += currentWeight;

			        velocity = SampleMotionVectors(uvWithOffset) * velocityFactor;
			        currentWeight *= _ToonRP_MotionBlur_WeightChangeRate;
			    }

			    color /= totalWeight;
			    
                return color;
            }

			float4 Frag(const Varyings IN) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                const float2 uv = UnityStereoTransformScreenSpaceTex(IN.texcoord);
                float3 color = Apply(uv);
                return float4(color, 1.0f);
            }

			ENDHLSL
		}
    }
}