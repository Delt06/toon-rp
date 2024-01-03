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
        [SerializeField] [Range(-1.0f, 1.0f)] private float _offsetMultiplier = 1.0f;
        [SerializeField] [Range(0.0f, 1.0f)] private float _saturation = 1.0f;
        [SerializeField] private ToonBlobShadowType _shadowType = ToonBlobShadowType.Circle;

        [SerializeField] [ToonRpShowIf(nameof(IsSquare))]
        private ToonBlobShadowSquareParams _square = new()
        {
            Width = 1.0f,
            Height = 1.0f,
            CornerRadius = 0.25f,
            Rotation = 0.0f,
        };
        [SerializeField] [ToonRpShowIf(nameof(IsBaked))]
        private ToonBlobShadowBakedParams _baked = new()
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

        public float OffsetMultiplier
        {
            get => _offsetMultiplier;
            set => _offsetMultiplier = value;
        }

        private Quaternion RotationWs => _transform.rotation;

        public ToonBlobShadowSquareParams Square
        {
            get => _square;
            set => _square = value;
        }

        public ToonBlobShadowBakedParams Baked
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

        public static bool ForceUpdateRenderers => !Application.isPlaying;

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

        private ToonBlobShadowPackedParams ConstructParams()
        {
            return _shadowType switch
            {
                ToonBlobShadowType.Circle => ToonBlobShadowPackedParams.PackCircle(
                    _offsetMultiplier, _saturation
                ),
                ToonBlobShadowType.Square => ToonBlobShadowPackedParams.PackSquare(_square, RotationWs.eulerAngles.y,
                    _offsetMultiplier, _saturation
                ),
                ToonBlobShadowType.Baked => ToonBlobShadowPackedParams.PackBaked(_baked, RotationWs.eulerAngles.y,
                    _offsetMultiplier, _saturation
                ),
                _ => throw new ArgumentOutOfRangeException(),
            };
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
            if (!_manager.TryGetGroup(_shadowType, out ToonBlobShadowsManager.Group group))
            {
                return;
            }

            ref ToonBlobShadowPackedData packedData =
                ref group.PackedDataPtr[Index];
            packedData.PositionSize = new half4(
                (half) rendererData.Position.x, (half) rendererData.Position.y,
                (half) rendererData.HalfSize, (half) rendererData.HalfSize
            );
            packedData.Params = rendererData.Params;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref ToonBlobShadowsRendererData GetRendererDataImpl() =>
            ref _manager.GetGroup(_shadowType).DataPtr[Index];

        private void RecomputeRendererData(ref ToonBlobShadowsRendererData rendererData, out bool changed,
            bool forceRecompute = false)
        {
            changed = false;

            if (_isStatic && !ForceUpdateRenderers && !forceRecompute && !_allDirty)
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
                changed = true;
            }

            if (forceRecompute || _allDirty || _paramsDirty || transformDirty)
            {
                rendererData.Params = ConstructParams();
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
    }
}