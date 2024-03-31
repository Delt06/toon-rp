#include_with_pragmas "ToonRPDefaultBaseShaderFeatureList.hlsl"

#pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
#pragma shader_feature_local_fragment _OVERRIDE_RAMP
#pragma shader_feature_local_fragment _RECEIVE_BLOB_SHADOWS

// Bug workaround: stencil might not be set if don't create a separate shader variant for outlines
#pragma shader_feature_local_vertex _STENCIL_OVERRIDE