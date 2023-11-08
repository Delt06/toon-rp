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

        public float HalfSize => _halfSize;

        public Vector3 Position => transform.position;

        public Vector4 Params => _shadowType switch
        {
            BlobShadowType.Circle => Vector4.zero,
            BlobShadowType.Square => _square.AsParams(transform.rotation),
            _ => throw new ArgumentOutOfRangeException(),
        };

        public BlobShadowType ShadowType => _shadowType;

        private bool IsSquare => _shadowType == BlobShadowType.Square;

        private void OnEnable()
        {
            BlobShadowsManager.OnRendererEnabled(this);
        }

        private void OnDisable()
        {
            BlobShadowsManager.OnRendererDisabled(this);
        }

        [Serializable]
        private struct SquareParams
        {
            [Range(0.0f, 1.0f)]
            public float Width;
            [Range(0.0f, 1.0f)]
            public float Height;
            [Range(0.0f, 1.0f)]
            public float CornerRadius;
            [Range(0.0f, 360.0f)]
            public float Rotation;

            public Vector4 AsParams(Quaternion rotation) => new(Width * 0.5f, Height * 0.5f, CornerRadius,
                (-rotation.eulerAngles.y + Rotation) / 360.0f % 1.0f
            );
        }
    }
}