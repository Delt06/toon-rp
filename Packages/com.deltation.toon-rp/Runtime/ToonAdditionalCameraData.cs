using DELTation.ToonRP.PostProcessing.BuiltIn;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class ToonAdditionalCameraData : MonoBehaviour, IAdditionalData
    {
        public ToonMotionVectorsPersistentData MotionVectorsPersistentData { get; } = new();
        public ToonTemporalAAPersistentData TemporalAAPersistentData { get; } = new();

        private void OnDestroy()
        {
            TemporalAAPersistentData.Dispose();
        }
    }
}