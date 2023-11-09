using System;
using DELTation.ToonRP.Attributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace DELTation.ToonRP.Shadows
{
    [ExecuteAlways]
    public sealed class BlobShadowRenderer : MonoBehaviour
    {
        [FormerlySerializedAs("_radius")] [SerializeField] [Min(0f)]
        private float _halfSize = 0.5f;
        [SerializeField] private BlobShadowType _shadowType = BlobShadowType.Circle;

        [SerializeField] [ToonRpShowIf(nameof(IsSquare))]
        private SquareParams _square = new()
        {
            Width = 1.0f,
            Height = 1.0f,
            CornerRadius = 0.25f,
            Rotation = 0.0f,
        };
        [SerializeField] [ToonRpShowIf(nameof(IsBaked))]
        private BakedParams _baked = new()
        {
            Rotation = 0.0f,
        };

        public float HalfSize
        {
            get => _halfSize;
            set => _halfSize = value;
        }

        public Vector3 Position
        {
            get => transform.position;
            set => transform.position = value;
        }

        public Vector4 Params => _shadowType switch
        {
            BlobShadowType.Circle => Vector4.zero,
            BlobShadowType.Square => _square.AsParams(transform.rotation),
            BlobShadowType.Baked => _baked.AsParams(transform.rotation),
            _ => throw new ArgumentOutOfRangeException(),
        };

        public SquareParams Square
        {
            get => _square;
            set => _square = value;
        }

        public BakedParams Baked
        {
            get => _baked;
            set => _baked = value;
        }

        public BlobShadowType ShadowType
        {
            get => _shadowType;
            set => _shadowType = value;
        }

        private bool IsSquare => _shadowType == BlobShadowType.Square;
        private bool IsBaked => _shadowType == BlobShadowType.Baked;

        private void OnEnable()
        {
            BlobShadowsManager.OnRendererEnabled(this);
        }

        private void OnDisable()
        {
            BlobShadowsManager.OnRendererDisabled(this);
        }

        private static float PackRotation(in Quaternion transformRotation, float paramsRotation) =>
            (-transformRotation.eulerAngles.y + paramsRotation) / 360.0f % 1.0f;

        [Serializable]
        public struct SquareParams
        {
            [Range(0.0f, 1.0f)]
            public float Width;
            [Range(0.0f, 1.0f)]
            public float Height;
            [Range(0.0f, 1.0f)]
            public float CornerRadius;
            [Range(0.0f, 360.0f)]
            public float Rotation;

            public Vector4 AsParams(in Quaternion rotation) => new(
                Width * 0.5f, Height * 0.5f, CornerRadius,
                PackRotation(rotation, Rotation)
            );
        }

        [Serializable]
        public struct BakedParams
        {
            public Texture2D BakedShadowTexture;
            [Range(0.0f, 360.0f)]
            public float Rotation;

            public Vector4 AsParams(in Quaternion rotation) => new(
                0.0f, 0.0f, 0.0f,
                PackRotation(rotation, Rotation)
            );
        }
    }
}