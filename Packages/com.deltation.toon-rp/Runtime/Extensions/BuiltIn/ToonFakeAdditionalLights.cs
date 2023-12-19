using System.Runtime.InteropServices;
using DELTation.ToonRP.Shadows.Blobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.Mathematics.math;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    public class ToonFakeAdditionalLights : ToonRenderingExtensionBase
    {
        private const int BatchSize = 256;
        public const string ShaderName = "Hidden/Toon RP/Fake Additional Lights";
        
        private static readonly int TextureId = Shader.PropertyToID("_FakeAdditionalLightsTexture");

        private readonly Vector4[] _batchLightsData = new Vector4[BatchSize];
        private Camera _camera;
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;
        private Material _material;
        private ToonFakeAdditionalLightsSettings _settings;

        public override void Setup(in ToonRenderingExtensionContext context,
            IToonRenderingExtensionSettingsStorage settingsStorage)
        {
            base.Setup(in context, settingsStorage);
            _context = context.ScriptableRenderContext;
            _cullingResults = context.CullingResults;
            _settings = settingsStorage.GetSettings<ToonFakeAdditionalLightsSettings>(this);
            _camera = context.Camera;
        }

        public override void Render()
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.FakeAdditionalLights)))
            {
                NativeList<Vector4> allLightsData = CollectLights();

                int textureSize = (int) _settings.Size;

                cmd.GetTemporaryRT(TextureId,
                    new RenderTextureDescriptor(textureSize, textureSize, RenderTextureFormat.ARGB32, 0, 1,
                        RenderTextureReadWrite.Linear
                    ), FilterMode.Bilinear
                );
                cmd.SetRenderTarget(TextureId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                cmd.ClearRenderTarget(false, true, Color.black);

                Bounds2D? intersection = FrustumPlaneProjectionUtils.ComputeFrustumPlaneIntersection(_camera,
                    _settings.MaxDistance,
                    _settings.ReceiverPlaneY
                );
                if (intersection == null)
                {
                    return;
                }

                Bounds2D receiverBounds = intersection.Value;

                {
                    float2 min = receiverBounds.Min;
                    float2 size = receiverBounds.Size;

                    float2 multiplier = float2(1.0f / size.x, 1.0f / size.y);
                    float2 offset = float2(-min.x, -min.y) * multiplier;

                    cmd.SetGlobalVector("_ToonRP_FakeAdditionalLights_Bounds_MultiplierOffset",
                        float4(multiplier, offset)
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

                    cmd.SetGlobalVectorArray("_FakeAdditionalLights", _batchLightsData);

                    EnsureMaterialIsCreated();
                    cmd.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Quads,
                        4 * count
                    );
                }
            }

            _context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void EnsureMaterialIsCreated()
        {
            if (_material == null)
            {
                _material = ToonRpUtils.CreateEngineMaterial(ShaderName, "Fake Additional Lights");
            }
        }

        private NativeList<Vector4> CollectLights()
        {
            var allLightsData = new NativeList<Vector4>(_cullingResults.visibleLights.Length, Allocator.Temp);

            foreach (VisibleLight visibleLight in _cullingResults.visibleLights)
            {
                if (visibleLight is
                    { lightType: LightType.Directional } or
                    { light: { lightmapBakeType: LightmapBakeType.Baked } })
                {
                    continue;
                }

                allLightsData.Add(PackLight(visibleLight));
            }

            return allLightsData;
        }

        private static Vector4 PackLight(in VisibleLight visibleLight)
        {
            Vector3 position = visibleLight.localToWorldMatrix.GetColumn(3);
            Color finalColor = visibleLight.finalColor;

            var packedData = new PackedLightData
            {
                // 1
                Word0 = Mathf.FloatToHalf(position.x),
                Word1 = Mathf.FloatToHalf(position.z),
                // 2
                Word2 = Mathf.FloatToHalf(visibleLight.range),
                Word3 = Mathf.FloatToHalf(1.0f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f)),
                // 3
                Word4 = Mathf.FloatToHalf(finalColor.r),
                Word5 = Mathf.FloatToHalf(finalColor.g),
                // 4
                Word6 = Mathf.FloatToHalf(finalColor.b),
            };
            return packedData.Vector;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct PackedLightData
        {
            [FieldOffset(0)] public Vector4 Vector;

            [FieldOffset(0)]
            public ushort Word0;
            [FieldOffset(2)]
            public ushort Word1;
            [FieldOffset(4)]
            public ushort Word2;
            [FieldOffset(6)]
            public ushort Word3;
            [FieldOffset(8)]
            public ushort Word4;
            [FieldOffset(10)]
            public ushort Word5;
            [FieldOffset(12)]
            public ushort Word6;
            [FieldOffset(14)]
            public ushort Word7;
        }
    }
}