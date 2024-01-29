#ifndef SMEAR_HLSL
#define SMEAR_HLSL

// https://github.com/cjacobwade/HelpfulScripts/blob/master/SmearEffect/Smear.shader

float Hash(const float n)
{
    return frac(sin(n) * 43758.5453);
}

void SmearPositionNoise_float(const float3 position, out float noise)
{
    // The noise function returns a value in the range -1.0f -> 1.0f

    float3 p = floor(position);
    float3 f = frac(position);

    f = f * f * (3.0 - 2.0 * f);
    const float n = p.x + p.y * 57.0 + 113.0 * p.z;

    noise = lerp(lerp(lerp(Hash(n + 0.0), Hash(n + 1.0), f.x),
                      lerp(Hash(n + 57.0), Hash(n + 58.0), f.x), f.y),
                 lerp(lerp(Hash(n + 113.0), Hash(n + 114.0), f.x),
                      lerp(Hash(n + 170.0), Hash(n + 171.0), f.x), f.y), f.z);
}

#endif // SMEAR_HLSL