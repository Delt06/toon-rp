using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Shadows
{
    public static class ToonShadowLightsUtils
    {
        public const int MaxSlicesPerLight = 4;

        public static void Init(ref Data data, int maxDirectionalLightShadows, int maxAdditionalLightShadows)
        {
            int lightsCount = data.CullingResults.visibleLights.Length;

            Assert.IsFalse(data.ShadowCastersCullingInfos.splitBuffer.IsCreated);
            Assert.IsFalse(data.ShadowCastersCullingInfos.perLightInfos.IsCreated);

            int maxTotalShadowLights = maxAdditionalLightShadows + maxDirectionalLightShadows;
            data.ShadowCastersCullingInfos = new ShadowCastersCullingInfos
            {
                splitBuffer = new NativeArray<ShadowSplitData>(MaxSlicesPerLight * math.min(lightsCount, maxTotalShadowLights), data.Allocator, NativeArrayOptions.UninitializedMemory),
                perLightInfos = new NativeArray<LightShadowCasterCullingInfo>(lightsCount, Allocator.Persistent, NativeArrayOptions.ClearMemory),
            };
            data.CurrentShadowSplitBufferOffset = 0;

            data.DirectionalLights = new NativeList<LightInfo>(maxDirectionalLightShadows, data.Allocator);
            data.AdditionalLights = new NativeList<LightInfo>(maxAdditionalLightShadows, data.Allocator);
        }

        public static void AddDirectionLightShadowInfo(ref Data data, int visibleLightIndex, Vector3 ratios, int cascadeCount, int shadowResolution, float nearPlaneOffset)
        {
            Assert.IsTrue(data.DirectionalLights.IsCreated);

            var lightInfo = new LightInfo
            {
                VisibleLightIndex = visibleLightIndex,
                Slices = new NativeArray<LightSliceInfo>(cascadeCount, data.Allocator, NativeArrayOptions.UninitializedMemory),
            };

            var splitRange = new RangeInt(data.CurrentShadowSplitBufferOffset, cascadeCount);

            for (int i = 0; i < cascadeCount; i++)
            {
                LightSliceInfo lightSliceInfo;

                bool computeSuccess = data.CullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                    visibleLightIndex, i, cascadeCount, ratios, shadowResolution, nearPlaneOffset,
                    out lightSliceInfo.ViewMatrix, out lightSliceInfo.ProjectionMatrix, out ShadowSplitData splitData
                );
                Assert.IsTrue(computeSuccess);

                data.ShadowCastersCullingInfos.splitBuffer[data.CurrentShadowSplitBufferOffset] = splitData;
                ++data.CurrentShadowSplitBufferOffset;

                lightInfo.Slices[i] = lightSliceInfo;
            }

            data.ShadowCastersCullingInfos.perLightInfos[visibleLightIndex] = new LightShadowCasterCullingInfo
            {
                projectionType = BatchCullingProjectionType.Orthographic,
                splitRange = splitRange,
            };
            data.DirectionalLights.Add(lightInfo);
        }

        public static bool TryAddSpotLightShadowInfo(ref Data data, int visibleLightIndex)
        {
            Assert.IsTrue(data.AdditionalLights.IsCreated);

            LightSliceInfo lightSliceInfo;

            if (data.CullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(visibleLightIndex, out lightSliceInfo.ViewMatrix, out lightSliceInfo.ProjectionMatrix, out ShadowSplitData splitData))
            {
                return false;
            }

            const int slices = 1;
            var splitRange = new RangeInt(data.CurrentShadowSplitBufferOffset, slices);

            data.ShadowCastersCullingInfos.splitBuffer[data.CurrentShadowSplitBufferOffset] = splitData;
            ++data.CurrentShadowSplitBufferOffset;

            data.ShadowCastersCullingInfos.perLightInfos[visibleLightIndex] = new LightShadowCasterCullingInfo
            {
                projectionType = BatchCullingProjectionType.Perspective,
                splitRange = splitRange,
            };

            var lightInfo = new LightInfo
            {
                VisibleLightIndex = visibleLightIndex,
                Slices = new NativeArray<LightSliceInfo>(slices, data.Allocator, NativeArrayOptions.UninitializedMemory)
                {
                    [0] = lightSliceInfo,
                },
            };
            data.AdditionalLights.Add(lightInfo);
            return true;
        }

        public static void CullShadowCasters(ref Data data)
        {
            data.ScriptableRenderContext.CullShadowCasters(data.CullingResults, data.ShadowCastersCullingInfos);
        }

        public struct Data
        {
            public ScriptableRenderContext ScriptableRenderContext;
            public CullingResults CullingResults;
            public readonly Allocator Allocator;

            public ShadowCastersCullingInfos ShadowCastersCullingInfos;
            public int CurrentShadowSplitBufferOffset;
            public NativeList<LightInfo> DirectionalLights;
            public NativeList<LightInfo> AdditionalLights;

            public Data(ScriptableRenderContext scriptableRenderContext, CullingResults cullingResults, Allocator allocator)
            {
                ScriptableRenderContext = scriptableRenderContext;
                CullingResults = cullingResults;
                Allocator = allocator;
                ShadowCastersCullingInfos = default;
                CurrentShadowSplitBufferOffset = 0;
                DirectionalLights = default;
                AdditionalLights = default;
            }
        }

        public struct LightInfo
        {
            public int VisibleLightIndex;
            public NativeArray<LightSliceInfo> Slices;
        }

        public struct LightSliceInfo
        {
            public Matrix4x4 ViewMatrix;
            public Matrix4x4 ProjectionMatrix;
        }
    }
}