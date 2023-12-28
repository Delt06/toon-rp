#ifndef TOON_RP_INVERTED_HULL_OUTLINE_MULTI_COMPILE_LIST
#define TOON_RP_INVERTED_HULL_OUTLINE_MULTI_COMPILE_LIST

//#pragma enable_d3d11_debug_symbols

#pragma multi_compile_local_vertex _ _NOISE
#pragma multi_compile_local_vertex _ _DISTANCE_FADE
#pragma multi_compile_local_vertex _ _NORMAL_SEMANTIC_UV2 _NORMAL_SEMANTIC_TANGENT
#pragma multi_compile_local_vertex _ _FIXED_SCREEN_SPACE_THICKNESS

#ifdef _NORMAL_SEMANTIC_UV2
#define NORMAL_SEMANTIC TEXCOORD2
#endif // _NORMAL_SEMANTIC_UV2

#ifdef _NORMAL_SEMANTIC_TANGENT
#define NORMAL_SEMANTIC TANGENT
#endif // _NORMAL_SEMANTIC_TANGENT

#endif // TOON_RP_INVERTED_HULL_OUTLINE_MULTI_COMPILE_LIST