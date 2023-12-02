﻿using System;
using DELTation.ToonRP.Attributes;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace DELTation.ToonRP.Shadows.Blobs
{
    [ExecuteAlways]
    public sealed unsafe class ToonBlobShadowRenderer : MonoBehaviour
    {
        [SerializeField] private bool _isStatic;

        [FormerlySerializedAs("_radius")] [SerializeField] [Min(0f)]
        private float _halfSize = 0.5f;
        [SerializeField] private ToonBlobShadowType _shadowType = ToonBlobShadowType.Circle;

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
        private bool _allDirty = true;

        private ToonBlobShadowsManager _manager;
        private bool _paramsDirty = true;
        private Transform _transform;

        public float HalfSize
        {
            get => _halfSize;
            set => _halfSize = value;
        }

        public Vector4 Params => _shadowType switch
        {
            ToonBlobShadowType.Circle => Vector4.zero,
            ToonBlobShadowType.Square => _square.AsParams(_transform.rotation),
            ToonBlobShadowType.Baked => _baked.AsParams(_transform.rotation),
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

        public ToonBlobShadowType ShadowType
        {
            get => _shadowType;
            set => _shadowType = value;
        }

        private bool IsSquare => _shadowType == ToonBlobShadowType.Square;
        private bool IsBaked => _shadowType == ToonBlobShadowType.Baked;

        public int Index { get; internal set; } = -1;

        public bool IsStatic
        {
            get => _isStatic;
            set
            {
                if (_isStatic == value)
                {
                    return;
                }

                _isStatic = value;
                if (_manager != null)
                {
                    _manager.UpdateStaticStatus(this);
                }
            }
        }

        private void Awake()
        {
            _transform = transform;
        }

        private void OnEnable()
        {
            ToonBlobShadowsManager.OnRendererEnabled(this);
        }

        private void OnDisable()
        {
            ToonBlobShadowsManager.OnRendererDisabled(this);
        }

        private void OnValidate()
        {
            MarkParamsDirty();
            if (_manager != null)
            {
                _manager.ForceUpdateStaticStatus(this);
            }
        }

        public void Init(ToonBlobShadowsManager manager, int index)
        {
            _manager = manager;
            Index = index;
            RecomputeRendererData(ref _manager.DataPtr[Index], true);
        }

        public ref readonly ToonBlobShadowsRendererData GetRendererData()
        {
            ref ToonBlobShadowsRendererData rendererData = ref _manager.DataPtr[Index];
            RecomputeRendererData(ref rendererData);
            return ref rendererData;
        }

        public void Shutdown()
        {
            _manager = null;
            Index = -1;
        }

        private static float PackRotation(in Quaternion transformRotation, float paramsRotation) =>
            (-transformRotation.eulerAngles.y + paramsRotation) / 360.0f % 1.0f;

        private void RecomputeRendererData(ref ToonBlobShadowsRendererData rendererData, bool forceRecompute = false)
        {
            if (_isStatic && !forceRecompute && !_allDirty)
            {
                return;
            }

            bool transformDirty = _transform.hasChanged;

            if (forceRecompute || _allDirty || transformDirty)
            {
                Vector3 transformPosition = _transform.position;
                rendererData.Position = new float2(transformPosition.x, transformPosition.z);
            }

            if (forceRecompute || _allDirty || _paramsDirty)
            {
                rendererData.HalfSize = _halfSize;
                rendererData.ShadowType = ShadowType;
                rendererData.BakedShadowTexture = ShadowType == ToonBlobShadowType.Baked
                    ? _baked.BakedShadowTexture
                    : null;
            }

            if (forceRecompute || _allDirty || _paramsDirty || transformDirty)
            {
                rendererData.Params = Params;
                rendererData.Bounds = new Bounds2D(rendererData.Position, _halfSize);
            }

            _paramsDirty = false;
            _allDirty = false;

            if (transformDirty)
            {
                _transform.hasChanged = false;
            }
        }

        public void MarkParamsDirty() => _paramsDirty = true;
        public void MarkAllDirty() => _allDirty = true;

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