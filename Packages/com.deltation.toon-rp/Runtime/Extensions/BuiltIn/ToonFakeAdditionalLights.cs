using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DELTation.ToonRP.Shadows.Blobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static DELTation.ToonRP.Shadows.Blobs.ToonPackingUtility;
using static Unity.Mathematics.math;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    public sealed class ToonFakeAdditionalLights : ToonRenderingExtensionBase
    {
        private const int BatchSize = 256;
        public const string ShaderName = "Hidden/Toon RP/Fake Additional Lights";

        private readonly Vector4[] _batchLightsData = new Vector4[BatchSize];
        private readonly ToonPipelineMaterial _material = new(ShaderName, "Toon RP Fake Additional Lights");
        private Camera _camera;
        private ToonCameraRendererSettings _cameraRendererSettings;
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;
        private ToonFakeAdditionalLightsSettings _settings;

        public override void Dispose()
        {
            base.Dispose();
            _material.Dispose();
        }

        public override void Setup(in ToonRenderingExtensionContext context,
            IToonRenderingExtensionSettingsStorage settingsStorage)
        {
            base.Setup(in context, settingsStorage);
            _context = context.ScriptableRenderContext;
            _cullingResults = context.CullingResults;
            _settings = settingsStorage.GetSettings<ToonFakeAdditionalLightsSettings>(this);
            _camera = context.Camera;
            _cameraRendererSettings = context.CameraRendererSettings;
        }

        public override void Render()
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.FakeAdditionalLights)))
            {
                NativeList<Vector4> allLightsData = CollectLights();

                int2 textureSize = (int) _settings.Size;
                Bounds2D receiverBounds;
                Bounds2D? receiverIntersectionBounds = null;

                // Render fake lights only if the traditional lights are disabled
                if (_cameraRendererSettings.AdditionalLights == ToonCameraRendererSettings.AdditionalLightsMode.Off)
                {
                    receiverIntersectionBounds = FrustumPlaneProjectionUtils.ComputeFrustumPlaneIntersection(_camera,
                        _settings.MaxDistance,
                        _settings.ReceiverPlaneY
                    );
                }

                if (receiverIntersectionBounds != null)
                {
                    receiverBounds = receiverIntersectionBounds.Value;
                    // Padding
                    receiverBounds.Size *= 1.0f + float2(1.0f) / textureSize;

                    // Adaptively reduce the lesser dimension
                    if (receiverBounds.Size.x < receiverBounds.Size.y)
                    {
                        textureSize.x = (int) ceil(textureSize.x * receiverBounds.Size.x / receiverBounds.Size.y);
                    }
                    else
                    {
                        textureSize.y = (int) ceil(textureSize.y * receiverBounds.Size.y / receiverBounds.Size.x);
                    }

                    textureSize = max(textureSize, 1);
                }
                else
                {
                    receiverBounds = default;
                    textureSize = 1;
                }

                cmd.GetTemporaryRT(ShaderIds.TextureId,
                    new RenderTextureDescriptor(textureSize.x, textureSize.y, RenderTextureFormat.ARGB32, 0, 1,
                        RenderTextureReadWrite.Linear
                    ), FilterMode.Bilinear
                );
                cmd.SetRenderTarget(ShaderIds.TextureId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                );
                cmd.ClearRenderTarget(false, true, Color.clear);

                if (receiverIntersectionBounds == null)
                {
                    return;
                }

                {
                    float2 min = receiverBounds.Min;
                    float2 size = receiverBounds.Size;

                    float2 multiplier = float2(1.0f / size.x, 1.0f / size.y);
                    float2 offset = float2(-min.x, -min.y) * multiplier;

                    cmd.SetGlobalVector(ShaderIds.BoundsMultiplierOffsetId,
                        float4(multiplier, offset)
                    );
                    cmd.SetGlobalFloat(ShaderIds.ReceiverPlaneYId, _settings.ReceiverPlaneY);
                    cmd.SetGlobalVector(ShaderIds.RampId, ToonRpUtils.BuildRampVectorFromSmoothness(
                            _settings.Threshold,
                            _settings.Smoothness
                        )
                    );
                }

                for (int startIndex = 0; startIndex < allLightsData.Length; startIndex += BatchSize)
                {
                    int endIndex = Mathf.Min(startIndex + BatchSize, allLightsData.Length);
                    int count = endIndex - startIndex;
                    if (count == 0)
                    {
                        break;
                    }

                    unsafe
                    {
                        fixed (Vector4* destination = _batchLightsData)
                        {
                            Vector4* source = allLightsData.GetUnsafePtr() + startIndex;
                            UnsafeUtility.MemCpy(destination, source, count * UnsafeUtility.SizeOf<Vector4>());
                        }
                    }

                    cmd.SetGlobalVectorArray(ShaderIds.LightsBufferId, _batchLightsData);

                    cmd.DrawProcedural(Matrix4x4.identity, _material.GetOrCreate(), 0, MeshTopology.Quads,
                        4 * count
                    );
                }

                cmd.SetGlobalVector(ShaderIds.FadesId,
                    float4(
                        PackFade(_settings.MaxDistance * _settings.MaxDistance, _settings.DistanceFade),
                        PackFade(_settings.MaxHeight, _settings.HeightFade)
                    )
                );
                cmd.SetGlobalFloat(ShaderIds.IntensityId, _settings.Intensity);
            }

            _context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private static float2 PackFade(float maxDistance, float distanceFade) =>
            1.0f / float2(maxDistance, distanceFade);

        private unsafe NativeList<Vector4> CollectLights()
        {
            var allLightsData = new NativeList<Vector4>(_cullingResults.visibleLights.Length, Allocator.Temp);

            int count = _cullingResults.visibleLights.Length;

            var visibleLightsPtr = (VisibleLight*) _cullingResults.visibleLights.GetUnsafePtr();

            Transform cameraTransform = _camera.transform;
            Vector3 cameraPosition = cameraTransform.position + cameraTransform.forward * _camera.nearClipPlane;
            float maxDistanceSqr = _settings.MaxDistance * _settings.MaxDistance;

            for (int index = 0; index < count; index++)
            {
                ref VisibleLight visibleLight = ref visibleLightsPtr[index];
                if (visibleLight is
                    { lightType: LightType.Directional }
#if UNITY_EDITOR
                    or { light: { lightmapBakeType: LightmapBakeType.Baked } }
#endif // UNITY_EDITOR
                   )
                {
                    continue;
                }

                Matrix4x4 localToWorldMatrix = visibleLight.localToWorldMatrix;
                Vector4 position = localToWorldMatrix.GetColumn(3);

                if (FastAbs(position.y - _settings.ReceiverPlaneY) > visibleLight.range)
                {
                    continue;
                }

                float cameraDistanceSqr = DistanceSqr(cameraPosition, position);
                if (cameraDistanceSqr > maxDistanceSqr)
                {
                    continue;
                }

                allLightsData.Add(PackLight(ref visibleLight, ref position, ref localToWorldMatrix));
            }

            return allLightsData;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float DistanceSqr(Vector3 position1, Vector4 position2)
        {
            float dx = position1.x - position2.x;
            float dy = position1.y - position2.y;
            float dz = position1.z - position2.z;
            return dx * dx + dy * dy + dz * dz;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float FastAbs(float value) => value < 0.0f ? -value : value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float FastMin(float x, float y) => x < y ? x : y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector4 PackLight(ref VisibleLight visibleLight, ref Vector4 position,
            ref Matrix4x4 localToWorldMatrix)
        {
            Color finalColor = visibleLight.finalColor.linear;

            LightType lightType = visibleLight.lightType;

            byte type = (byte) lightType;

            var packedData = new PackedLightData
            {
                Bytes_00_01 = FloatToHalfFast(position.x),
                Bytes_02_03 = FloatToHalfFast(position.y),
                Bytes_04_05 = FloatToHalfFast(position.z),
                Bytes_06_07 = FloatToHalfFast(visibleLight.range),

                // limiting the upper bound is enough
                Byte_08 = PackAsUNormUnclamped(FastMin(finalColor.r, 1.0f)),
                Byte_09 = PackAsUNormUnclamped(FastMin(finalColor.g, 1.0f)),
                Byte_10 = PackAsUNormUnclamped(FastMin(finalColor.b, 1.0f)),
            };

            if (lightType == LightType.Spot)
            {
                Vector4 direction = localToWorldMatrix.GetColumn(2);

                packedData.Byte_11 = PackAsSNorm(direction.x);
                packedData.Byte_12 = PackAsSNorm(direction.y);
                packedData.Byte_13 = PackAsSNorm(direction.z);
                packedData.Byte_14 = PackAsUNorm(cos(radians(visibleLight.spotAngle * 0.5f)));
            }

            packedData.Byte_15 = type;

            return packedData.Vector;
        }


        private static class ShaderIds
        {
            public static readonly int LightsBufferId = Shader.PropertyToID("_FakeAdditionalLights");

            public static readonly int TextureId = Shader.PropertyToID("_FakeAdditionalLightsTexture");
            public static readonly int BoundsMultiplierOffsetId =
                Shader.PropertyToID("_ToonRP_FakeAdditionalLights_Bounds_MultiplierOffset");
            public static readonly int ReceiverPlaneYId =
                Shader.PropertyToID("_ToonRP_FakeAdditionalLights_ReceiverPlaneY");
            public static readonly int RampId = Shader.PropertyToID("_ToonRP_FakeAdditionalLights_Ramp");
            public static readonly int FadesId = Shader.PropertyToID("_ToonRP_FakeAdditionalLights_Fades");
            public static readonly int IntensityId = Shader.PropertyToID("_ToonRP_FakeAdditionalLights_Intensity");
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct PackedLightData
        {
            [FieldOffset(0)] public Vector4 Vector;

            [FieldOffset(00)] public ushort Bytes_00_01;
            [FieldOffset(02)] public ushort Bytes_02_03;
            [FieldOffset(04)] public ushort Bytes_04_05;
            [FieldOffset(06)] public ushort Bytes_06_07;
            [FieldOffset(08)] public ushort Bytes_08_09;
            [FieldOffset(10)] public ushort Bytes_10_11;
            [FieldOffset(12)] public ushort Bytes_12_13;
            [FieldOffset(14)] public ushort Bytes_14_15;

            [FieldOffset(00)] public byte Byte_00;
            [FieldOffset(01)] public byte Byte_01;
            [FieldOffset(02)] public byte Byte_02;
            [FieldOffset(03)] public byte Byte_03;
            [FieldOffset(04)] public byte Byte_04;
            [FieldOffset(05)] public byte Byte_05;
            [FieldOffset(06)] public byte Byte_06;
            [FieldOffset(07)] public byte Byte_07;
            [FieldOffset(08)] public byte Byte_08;
            [FieldOffset(09)] public byte Byte_09;
            [FieldOffset(10)] public byte Byte_10;
            [FieldOffset(11)] public byte Byte_11;
            [FieldOffset(12)] public byte Byte_12;
            [FieldOffset(13)] public byte Byte_13;
            [FieldOffset(14)] public byte Byte_14;
            [FieldOffset(15)] public byte Byte_15;
        }
    }
}