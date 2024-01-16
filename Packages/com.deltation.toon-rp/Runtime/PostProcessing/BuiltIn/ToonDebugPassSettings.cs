using System;
using DELTation.ToonRP.Attributes;
using UnityEngine;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [Serializable]
    public struct ToonDebugPassSettings
    {
        public enum DebugMode
        {
            None,
            TiledLighting,
            Depth,
            Normals,
            MotionVectors,
        }

        public DebugMode Mode;

        [ToonRpShowIf(nameof(MotionVectorsOn))]
        public MotionVectorsSettings MotionVectors;

        private bool MotionVectorsOn => Mode == DebugMode.MotionVectors;

        public bool IsEffectivelyEnabled()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return Mode != DebugMode.None;
#else
            return false;
#endif
        }

        [Serializable]
        public struct MotionVectorsSettings
        {
            [Min(0.0f)]
            public float Scale;
            [Range(0.0f, 1.0f)]
            public float SceneIntensity;
        }
    }
}