//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef TILEDLIGHT_CS_HLSL
#define TILEDLIGHT_CS_HLSL
// Generated from DELTation.ToonRP.Lighting.TiledLight
// PackingRules = Exact
struct TiledLight
{
    float4 Color;
    float4 BoundingSphere_CenterVs_Radius;
    float4 PositionWs_Attenuation;
    float4 ConeBoundingSphere_CenterVs_Radius;
};

//
// Accessors for DELTation.ToonRP.Lighting.TiledLight
//
float4 GetColor(TiledLight value)
{
    return value.Color;
}
float4 GetBoundingSphere_CenterVs_Radius(TiledLight value)
{
    return value.BoundingSphere_CenterVs_Radius;
}
float4 GetPositionWs_Attenuation(TiledLight value)
{
    return value.PositionWs_Attenuation;
}
float4 GetConeBoundingSphere_CenterVs_Radius(TiledLight value)
{
    return value.ConeBoundingSphere_CenterVs_Radius;
}

#endif
