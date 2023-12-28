#ifndef SHADER_GRAPH_FORWARD_DECLARATIONS_HLSL
#define SHADER_GRAPH_FORWARD_DECLARATIONS_HLSL

PackedVaryings PackVaryings(Varyings varyings);
Varyings UnpackVaryings(PackedVaryings packedVaryings);
Varyings BuildVaryings(Attributes input, out VertexDescription vertexDescription, out float3 positionWS, out float3 normalWS);

SurfaceDescription BuildSurfaceDescription(Varyings varyings);

#endif // SHADER_GRAPH_FORWARD_DECLARATIONS_HLSL