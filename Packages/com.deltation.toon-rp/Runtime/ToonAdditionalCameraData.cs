using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public sealed class ToonAdditionalCameraData : MonoBehaviour, IAdditionalData
    {
        private readonly Dictionary<Type, object> _persistentDataStorage = new();
        public Camera Camera { get; private set; }

        public Matrix4x4 BaseProjectionMatrix { get; set; }
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

            RTHandleSystem.Dispose();
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