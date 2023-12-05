using System;
using System.Runtime.CompilerServices;
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

        internal ToonBlobShadowType AssignedGroupShadowType { get; private set; } =
            (ToonBlobShadowType) ToonBlobShadowTypes.Count;

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
                ReRegister();
            }
        }

        private void Awake()
        {
            _transform = transform;
        }

        private void OnEnable()
        {
            ToonBlobShadowsManagers.OnRendererEnabled(this);
        }

        private void OnDisable()
        {
            ToonBlobShadowsManagers.OnRendererDisabled(this);
        }

        private void OnValidate()
        {
            MarkParamsDirty();
            ReRegister();
        }

        private void ReRegister()
        {
            if (_manager != null && isActiveAndEnabled)
            {
                ToonBlobShadowsManagers.OnRendererDisabled(this);
                ToonBlobShadowsManagers.OnRendererEnabled(this);
            }
        }

        public void AssignToManager(ToonBlobShadowsManager manager, int index)
        {
            _manager = manager;
            AssignedGroupShadowType = _shadowType;
            Index = index;

            ref ToonBlobShadowsRendererData rendererData = ref GetRendererDataImpl();
            RecomputeRendererData(ref rendererData, out bool _, true);
            UpdatePackedData(rendererData);
        }

        public void UnassignFromManager()
        {
            _manager = null;
            AssignedGroupShadowType = (ToonBlobShadowType) ToonBlobShadowTypes.Count;
            Index = -1;
        }

        public void UpdateRendererData(out bool changed)
        {
            ref ToonBlobShadowsRendererData rendererData = ref GetRendererDataImpl();
            RecomputeRendererData(ref rendererData, out changed);

            if (changed)
            {
                UpdatePackedData(rendererData);
            }
        }

        private void UpdatePackedData(in ToonBlobShadowsRendererData rendererData)
        {
            ref ToonBlobShadowsManager.RendererPackedData packedData =
                ref _manager.GetGroup(_shadowType).PackedDataPtr[Index];
            packedData.PositionSize = new float4(
                rendererData.Position.x, rendererData.Position.y,
                rendererData.HalfSize, rendererData.HalfSize
            );
            packedData.Params = rendererData.Params;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly ToonBlobShadowsRendererData GetRendererData() => ref GetRendererDataImpl();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref ToonBlobShadowsRendererData GetRendererDataImpl() =>
            ref _manager.GetGroup(_shadowType).DataPtr[Index];

        private static float PackRotation(in Quaternion transformRotation, float paramsRotation) =>
            (-transformRotation.eulerAngles.y + paramsRotation) / 360.0f % 1.0f;

        private void RecomputeRendererData(ref ToonBlobShadowsRendererData rendererData, out bool changed,
            bool forceRecompute = false)
        {
            changed = false;

            if (_isStatic && !forceRecompute && !_allDirty)
            {
                return;
            }

            bool transformDirty = _transform.hasChanged;

            if (forceRecompute || _allDirty || transformDirty)
            {
                Vector3 transformPosition = _transform.position;
                rendererData.Position = new float2(transformPosition.x, transformPosition.z);
                changed = true;
            }

            if (forceRecompute || _allDirty || _paramsDirty)
            {
                rendererData.HalfSize = _halfSize;
                rendererData.ShadowType = ShadowType;
                changed = true;
            }

            if (forceRecompute || _allDirty || _paramsDirty || transformDirty)
            {
                rendererData.Params = Params;
                rendererData.Bounds = Bounds2D.FromCenterExtents(rendererData.Position, _halfSize);
                changed = true;
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
            [Range(0, ToonBlobShadows.MaxBakedTextures - 1)]
            public int TextureIndex;
            [Range(0.0f, 360.0f)]
            public float Rotation;

            public Vector4 AsParams(in Quaternion rotation) => new(
                TextureIndex, 0.0f, 0.0f,
                PackRotation(rotation, Rotation)
            );
        }
    }
}