using System;
using System.Collections.Generic;
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
    }
}