using DELTation.ToonRP.Shadows.Blobs;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace Samples.BlobShadows.Scripts
{
    public class BlobShadowsTestGroup : MonoBehaviour
    {
        [SerializeField] private float _maxDistance = 10.0f;
        [SerializeField] private float _minSize = 0.5f;
        [SerializeField] private float _maxSize = 2.0f;
        [SerializeField] private Bounds2D _bounds;

        private ToonBlobShadowsGroup _group;

        private Scene Scene => gameObject.scene;

        private void OnEnable()
        {
            EnsureGroupIsCreated();

            ToonBlobShadowsManagers.RegisterCustomGroup(Scene, _group);
        }

        private void OnDisable()
        {
            if (_group != null)
            {
                ToonBlobShadowsManagers.UnregisterCustomGroup(Scene, _group);
            }
        }

        private void OnDestroy()
        {
            if (_group != null)
            {
                ToonBlobShadowsManagers.UnregisterCustomGroup(Scene, _group);
                _group.Dispose();
                _group = null;
            }
        }

        private unsafe void EnsureGroupIsCreated()
        {
            if (_group == null)
            {
                _group = new ToonBlobShadowsGroup(ToonBlobShadowType.Square, _bounds);

                var packedDataPtr = (ToonBlobShadowPackedData*) _group.PackedData.GetUnsafePtr();

                for (int i = 0; i < _group.Capacity; i++)
                {
                    ref ToonBlobShadowPackedData packedData = ref packedDataPtr[i];

                    float angle = Random.Range(0.0f, 360.0f);
                    math.sincos(math.radians(angle), out float sin, out float cos);

                    var position = (half2) (math.float2(cos, sin) * Random.Range(0.0f, _maxDistance));
                    var size = (half) Random.Range(_minSize, _maxSize);

                    packedData.PositionSize = math.half4(position, size, size);
                    packedData.Params = ToonBlobShadowPackedParams.PackSquare(
                        new ToonBlobShadowSquareParams
                        {
                            Height = 1.0f,
                            Width = Random.Range(0.5f, 1.0f),
                            Rotation = Random.Range(0.0f, 360.0f),
                            CornerRadius = Random.Range(0.0f, 1.0f),
                        }
                    );

                    ++_group.Size;
                }

                _group.PushDataToGPU();
            }
        }
    }
}