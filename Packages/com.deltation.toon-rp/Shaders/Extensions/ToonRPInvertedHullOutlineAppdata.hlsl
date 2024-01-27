#ifndef TOON_RP_INVERTED_HULL_OUTLINE_APPDATA
#define TOON_RP_INVERTED_HULL_OUTLINE_APPDATA

#if !defined(NORMAL_SEMANTIC)
#define NORMAL_SEMANTIC NORMAL
#endif // !NORMAL_SEMANTIC

#ifndef EXTRA_APP_DATA
#define EXTRA_APP_DATA
#endif // !EXTRA_APP_DATA

struct appdata
{
    float3 vertex : POSITION;
    float3 normal : NORMAL_SEMANTIC;

    #ifdef _NOISE
    float2 uv : TEXCOORD0;
    #endif // _NOISE

    UNITY_VERTEX_INPUT_INSTANCE_ID

    EXTRA_APP_DATA
};

#ifdef _NOISE
#define TOON_RP_OUTLINES_UV(appdata) (appdata.uv)
#else // !_NOISE
#define TOON_RP_OUTLINES_UV(appdata) (float2(0, 0))
#endif // _NOISE

#endif // TOON_RP_INVERTED_HULL_OUTLINE_APPDATA