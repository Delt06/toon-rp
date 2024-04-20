#if defined(FEATURES_GRAPH_VERTEX)
#if defined(HAVE_VFX_MODIFICATION)
VertexDescription BuildVertexDescription(Attributes input, AttributesElement element)
{
    GraphProperties properties;
    ZERO_INITIALIZE(GraphProperties, properties);
    // Fetch the vertex graph properties for the particle instance.
    GetElementVertexProperties(element, properties);

    // Evaluate Vertex Graph
    VertexDescriptionInputs vertexDescriptionInputs = BuildVertexDescriptionInputs(input);
    VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs, properties);
    return vertexDescription;
}
#else
VertexDescription BuildVertexDescription(Attributes input)
{
    // Evaluate Vertex Graph
    VertexDescriptionInputs vertexDescriptionInputs = BuildVertexDescriptionInputs(input);
    VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);
    return vertexDescription;
}
#endif
#endif

#ifndef TRANSFORM_WORLD_TO_HCLIP
#define TRANSFORM_WORLD_TO_HCLIP(positionWS, normalWS, appdata, vertexDescription) TransformWorldToHClip(positionWS)
#endif // TRANSFORM_WORLD_TO_HCLIP

Varyings BuildVaryings(Attributes input, out VertexDescription vertexDescription, out float3 positionWS, out float3 normalWS)
{
    // ReSharper disable once CppRedundantCastExpression
    Varyings output = (Varyings) 0;

    UNITY_SETUP_INSTANCE_ID(input);

    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    #if defined(FEATURES_GRAPH_VERTEX)

    vertexDescription = BuildVertexDescription(input);

    #if defined(CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC)
        CustomInterpolatorPassThroughFunc(output, vertexDescription);
    #endif

    // Assign modified vertex attributes
    input.positionOS = vertexDescription.Position;
    #if defined(VARYINGS_NEED_NORMAL_WS)
        input.normalOS = vertexDescription.Normal;
    #endif //FEATURES_GRAPH_NORMAL
    #if defined(VARYINGS_NEED_TANGENT_WS)
        input.tangentOS.xyz = vertexDescription.Tangent.xyz;
    #endif //FEATURES GRAPH TANGENT

    #else // !FEATURES_GRAPH_VERTEX

    vertexDescription = (VertexDescription) 0;

    #endif //FEATURES_GRAPH_VERTEX

    // TODO: Avoid path via VertexPositionInputs (Universal)
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

    // Returns the camera relative position (if enabled)
    positionWS = TransformObjectToWorld(input.positionOS);
    
    #if _BILLBOARD
    {
        const float3 pivot = GetObjectPosition();
        const float3 pivotViewDir = normalize(GetWorldSpaceViewDir(pivot));
        positionWS = mul(UNITY_MATRIX_I_V, float4(input.positionOS * GetObjectScale(), 0)).xyz + pivot;
        positionWS += pivotViewDir * vertexDescription.BillboardCameraPull;
    }
    #endif // _BILLBOARD

    #ifdef ATTRIBUTES_NEED_NORMAL

    float3 normalOS;
    #if defined(SHADER_GRAPH_NORMAL_SOURCE_TANGENT)
    normalOS = input.tangentOS.xyz;
    #elif defined (SHADER_GRAPH_NORMAL_SOURCE_UV2)
    normalOS = input.uv2.xyz;
    #else
    normalOS = input.normalOS.xyz;
    #endif 
    
    normalWS = TransformObjectToWorldNormal(normalOS);
    #else
    // Required to compile ApplyVertexModification that doesn't use normal.
    normalWS = float3(0.0, 0.0, 0.0);
    #endif

    #ifdef ATTRIBUTES_NEED_TANGENT
    float4 tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
    #endif

    // TODO: Change to inline ifdef
    // Do vertex modification in camera relative space (if enabled)
    #if defined(HAVE_VERTEX_MODIFICATION)
    ApplyVertexModification(input, normalWS, positionWS, _TimeParameters.xyz);
    #endif

    #ifdef VARYINGS_NEED_POSITION_WS
    output.positionWS = positionWS;
    #endif

    #ifdef VARYINGS_NEED_NORMAL_WS
    output.normalWS = normalWS;         // normalized in TransformObjectToWorldNormal()
    #endif

    #ifdef VARYINGS_NEED_TANGENT_WS
    output.tangentWS = tangentWS;       // normalized in TransformObjectToWorldDir()
    #endif

    #if (SHADERPASS == SHADERPASS_SHADOWCASTER)
    
    positionWS = ApplyShadowBias(positionWS, normalWS, _DirectionalLightDirection);
    output.positionCS = TRANSFORM_WORLD_TO_HCLIP(positionWS, normalWS, input, vertexDescription);
    #if UNITY_REVERSED_Z
        output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
    #else
        output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
    #endif

    #ifdef _TOON_RP_VSM
    output.vsmDepth = GetPackedVsmDepth(positionWS);
    #endif // _TOON_RP_VSM

    #elif (SHADERPASS == SHADERPASS_META)
    output.positionCS = UnityMetaVertexPosition(input.positionOS.xyz, input.uv1.xy, input.uv2.xy);
    #else // (SHADERPASS != SHADERPASS_SHADOWCASTER && SHADERPASS != SHADERPASS_META)
    output.positionCS = TRANSFORM_WORLD_TO_HCLIP(positionWS, normalWS, input, vertexDescription);

    #if UNITY_REVERSED_Z
    output.positionCS.z -= vertexDescription.DepthBias * output.positionCS.w;
    #else
    output.positionCS.z += vertexDescription.DepthBias * output.positionCS.w;
    #endif
    
    #endif // (SHADERPASS == SHADERPASS_SHADOWCASTER)

    #if defined(VARYINGS_NEED_TEXCOORD0) || defined(VARYINGS_DS_NEED_TEXCOORD0)
    output.texCoord0 = input.uv0;
    #endif
    #ifdef EDITOR_VISUALIZATION
    UnityEditorVizData(input.positionOS, input.uv0, input.uv1, input.uv2, output.VizUV, output.LightCoord);
    #endif
    #if defined(VARYINGS_NEED_TEXCOORD1) || defined(VARYINGS_DS_NEED_TEXCOORD1)
    output.texCoord1 = input.uv1;
    #endif
    #if defined(VARYINGS_NEED_TEXCOORD2) || defined(VARYINGS_DS_NEED_TEXCOORD2)
    output.texCoord2 = input.uv2;
    #endif
    #if defined(VARYINGS_NEED_TEXCOORD3) || defined(VARYINGS_DS_NEED_TEXCOORD3)
    output.texCoord3 = input.uv3;
    #endif

    #if defined(VARYINGS_NEED_COLOR) || defined(VARYINGS_DS_NEED_COLOR)
    output.color = input.color;
    #endif

    #ifdef VARYINGS_NEED_VIEWDIRECTION_WS
    // Need the unnormalized direction here as otherwise interpolation is incorrect.
    // It is normalized after interpolation in the fragment shader.
    output.viewDirectionWS = GetWorldSpaceViewDir(positionWS);
    #endif

    #ifdef VARYINGS_NEED_SCREENPOSITION
    output.screenPosition = vertexInput.positionNDC;
    #endif

    #if defined(VARYINGS_NEED_FOG_AND_VERTEX_LIGHT) && (!_FORCE_DISABLE_FOG || defined(_TOON_RP_ADDITIONAL_LIGHTS_VERTEX))
    
    half fogFactor = ComputeFogFactor(output.positionCS.z);
    #ifdef SHADERGRAPH_PREVIEW
    fogFactor = 1.0;
    #endif // SHADERGRAPH_PREVIEW

    #if _FORCE_DISABLE_FOG
    fogFactor = 1.0;
    #endif // _FORCE_DISABLE_FOG
    
    #ifdef _TOON_RP_ADDITIONAL_LIGHTS_VERTEX
    LightComputationParameters lightComputationParameters = (LightComputationParameters) 0;
    lightComputationParameters.positionWs = positionWS;
    lightComputationParameters.positionCs = output.positionCS;
    lightComputationParameters.normalWs = normalWS;
    float3 vertexLight, vertexLightSpecularUnused;
    ComputeAdditionalLightsDiffuseSpecular(lightComputationParameters, 1, vertexLight, vertexLightSpecularUnused);
    #else
    const float3 vertexLight = 0;
    #endif // _TOON_RP_ADDITIONAL_LIGHTS_VERTEX
    output.fogFactorAndVertexLight = float4(fogFactor, vertexLight);
    
    #endif

    #if defined(VARYINGS_NEED_LIGHTMAP_UV)
    TOON_RP_GI_TRANSFER_ATT(input, output, uv1.xy);
    #endif
    
    #if defined(VARYINGS_NEED_SHADOW_COORD) && defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
    #endif

    return output;
}

SurfaceDescription BuildSurfaceDescription(Varyings varyings)
{
    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(varyings);
    #if defined(HAVE_VFX_MODIFICATION)
    GraphProperties properties;
    ZERO_INITIALIZE(GraphProperties, properties);
    GetElementPixelProperties(surfaceDescriptionInputs, properties);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs, properties);
    #else
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
    #endif
    return surfaceDescription;
}

float3 GetNormalWsFromVaryings(SurfaceDescription surfaceDescription, Varyings varyings)
{
    // Retrieve the normal from the bump map or mesh normal
    #ifdef VARYINGS_NEED_NORMAL_WS

    #if defined(_NORMALMAP) && (SHADERPASS != SHADERPASS_SHADOWCASTER) && (SHADERPASS != SHADERPASS_META)
    #if _NORMAL_DROPOFF_TS
    // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
    float crossSign = (varyings.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
    float3 bitangent = crossSign * cross(varyings.normalWS.xyz, varyings.tangentWS.xyz);
    float3 normalWS = TransformTangentToWorld(surfaceDescription.NormalTS, float3x3(varyings.tangentWS.xyz, bitangent, varyings.normalWS.xyz));
    #elif _NORMAL_DROPOFF_OS
    float3 normalWS = TransformObjectToWorldNormal(surfaceDescription.NormalOS);
    #elif _NORMAL_DROPOFF_WS
    float3 normalWS = surfaceDescription.NormalWS;
    #else
    float3 normalWS = varyings.normalWS;
    #endif
    #else
    float3 normalWS = varyings.normalWS;
    #endif
    normalWS = normalize(normalWS);
    
    #else

    float3 normalWS = 0;

    #endif // VARYINGS_NEED_NORMAL_WS

    return normalWS;
}

float3 GetPositionWsFromVaryings(SurfaceDescription surfaceDescription, Varyings varyings)
{
    #ifdef VARYINGS_NEED_POSITION_WS

    #if _POSITION_DROPOFF_WS
    float3 positionWS = surfaceDescription.PositionWS;
    #else
    float3 positionWS = varyings.positionWS;
    #endif
    
    #else

    float3 positionWS = 0;

    #endif // VARYINGS_NEED_POSITION_WS

    return positionWS;
}

void ApplyCustomFog(inout float3 outputColor, const SurfaceDescription surfaceDescription)
{
    #if _CUSTOM_FOG
    outputColor = lerp(outputColor, surfaceDescription.CustomFogColor, surfaceDescription.CustomFogFactor);
    #endif // _CUSTOM_FOG
}