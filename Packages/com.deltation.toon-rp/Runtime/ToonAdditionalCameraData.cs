using System;
using System.Collections.Generic;
using DELTation.ToonRP.Attributes;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public sealed class ToonAdditionalCameraData : MonoBehaviour, IAdditionalData
    {
        private readonly Dictionary<Type, object> _persistentDataStorage = new();

        [NonSerialized] [CanBeNull]
        public RTHandle IntermediateColorRt;
        [NonSerialized] [CanBeNull]
        public RTHandle IntermediateDepthRt;

        // Volume System
        [Header("Volume Stack")]
        [SerializeField] public LayerMask VolumeLayerMask = 1; // "Default"
        [HideInInspector]
        [SerializeField] public Transform VolumeTrigger = null;
        [SerializeField] public VolumeFrameworkUpdateMode VolumeFrameworkUpdateModeOption = VolumeFrameworkUpdateMode.UsePipelineSettings;


#if !(ENABLE_VR && ENABLE_XR_MODULE)
        [HideInInspector]
#endif // !(ENABLE_VR && ENABLE_XR_MODULE)
        public bool EnableXRRendering = true;

        public Camera Camera { get; private set; }
        public XRPass XrPass { get; internal set; } = XRSystem.emptyPass;

        public Matrix4x4 ViewMatrix { get; set; }
        public Matrix4x4 BaseProjectionMatrix { get; set; }
        public Matrix4x4 JitterMatrix { get; set; }
        public Matrix4x4 JitteredProjectionMatrix { get; set; }
        public Matrix4x4 JitteredGpuProjectionMatrix { get; set; }
        public int RtWidth { get; set; }
        public int RtHeight { get; set; }

        public bool UsingCustomProjection { get; private set; }
        public RTHandleSystem RTHandleSystem { get; } = new();
        public VolumeStack VolumeStack { get; private set; }


        private void Awake()
        {
            Camera = GetComponent<Camera>();
        }

        private void OnDestroy()
        {
            foreach (object data in _persistentDataStorage.Values)
            {
                if (data is IDisposable disposableData)
                {
                    disposableData.Dispose();
                }
            }

            _persistentDataStorage.Clear();

            RTHandleSystem.ReleaseIfAllocated(ref IntermediateColorRt);
            RTHandleSystem.ReleaseIfAllocated(ref IntermediateDepthRt);
            RTHandleSystem.Dispose();

            Camera.DestroyVolumeStack(this);
        }

        public Matrix4x4 GetViewMatrix(int viewIndex = 0)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (XrPass.enabled)
            {
                return XrPass.GetViewMatrix(viewIndex);
            }
#endif
            return ViewMatrix;
        }

        public Matrix4x4 GetProjectionMatrix(int viewIndex = 0)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (XrPass.enabled)
            {
                return JitterMatrix * XrPass.GetProjMatrix(viewIndex);
            }
#endif
            return JitteredProjectionMatrix;
        }

        internal Matrix4x4 GetProjectionMatrixNoJitter(int viewIndex = 0)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (XrPass.enabled)
            {
                return XrPass.GetProjMatrix(viewIndex);
            }
#endif
            return BaseProjectionMatrix;
        }

        public T GetPersistentData<T>() where T : class, new()
        {
            if (_persistentDataStorage.TryGetValue(typeof(T), out object data))
            {
                return (T) data;
            }

            var newData = new T();
            _persistentDataStorage.Add(typeof(T), newData);
            return newData;
        }

        public void SetCustomProjectionMatrix(Matrix4x4 projectionMatrix)
        {
            Camera.projectionMatrix = projectionMatrix;
            UsingCustomProjection = true;
        }

        public void RestoreProjection()
        {
            if (!UsingCustomProjection)
            {
                return;
            }

            UsingCustomProjection = false;
            Camera.ResetProjectionMatrix();
        }

        /// <summary>
        /// Returns true if this camera requires the volume framework to be updated every frame.
        /// </summary>
        public bool RequiresVolumeFrameworkUpdate
        {
            get
            {
                if (VolumeFrameworkUpdateModeOption == VolumeFrameworkUpdateMode.UsePipelineSettings)
                {
                    // TO-DO: Load settings from ToonRenderPipelineAsset
                    // return ToonRenderPipelineAsset.asset.volumeFrameworkUpdateMode != VolumeFrameworkUpdateMode.ViaScripting;
                }

                return VolumeFrameworkUpdateModeOption == VolumeFrameworkUpdateMode.EveryFrame;
            }
        }

        /// <summary>
        /// Container for volume stacks in order to reuse stacks and avoid
        /// creating new ones every time a new camera is instantiated.
        /// </summary>
        private static List<VolumeStack> _cachedVolumeStacks;

        /// <summary>
        /// Returns the current volume stack used by this camera.
        /// </summary>
        public VolumeStack volumeStack
        {
            get => _volumeStack;
            set
            {
                // If the volume stack is being removed,
                // add it back to the list so it can be reused later
                if (value == null && _volumeStack != null && _volumeStack.isValid)
                {
                    if (_cachedVolumeStacks == null)
                        _cachedVolumeStacks = new List<VolumeStack>(4);

                    _cachedVolumeStacks.Add(_volumeStack);
                }

                _volumeStack = value;
            }
        }
        VolumeStack _volumeStack = null;

        /// <summary>
        /// Tries to retrieve a volume stack from the container
        /// and creates a new one if that fails.
        /// </summary>
        internal void GetOrCreateVolumeStack()
        {
            // Try first to reuse a volume stack
            if (_cachedVolumeStacks != null && _cachedVolumeStacks.Count > 0)
            {
                int index = _cachedVolumeStacks.Count - 1;
                var stack = _cachedVolumeStacks[index];
                _cachedVolumeStacks.RemoveAt(index);
                if (stack.isValid)
                    volumeStack = stack;
            }

            // Create a new stack if was not possible to reuse an old one
            if (volumeStack == null)
                volumeStack = VolumeManager.instance.CreateStack();
        }
    }
}
