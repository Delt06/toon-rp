using System.Collections.Generic;
using UnityEngine;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    public class ToonScreenSpaceShakePersistentData : IToonPersistentCameraData
    {
        private readonly List<Shake> _shakes = new();

        public float Magnitude { get; set; } = 0.1f;
        public float Frequency { get; set; } = 20f;

        public float CurrentAmount { get; private set; }

        public void Update()
        {
            float totalShake = 0.0f;

            // Reverse order for easier removal
            for (int i = _shakes.Count - 1; i >= 0; i--)
            {
                Shake shake = _shakes[i];

                shake.Time += shake.UnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                totalShake += shake.ComputeCurrentStrength();

                if (shake.Time >= shake.Duration)
                {
                    _shakes.RemoveAt(i);
                }
                else
                {
                    _shakes[i] = shake;
                }
            }

            if (totalShake > 0)
            {
                CurrentAmount = Mathf.PerlinNoise(Time.time * Frequency, 10.234896f) * totalShake * Magnitude;
            }
            else
            {
                CurrentAmount = 0;
            }
        }

        public void AddShake(in Shake shake) =>
            _shakes.Add(shake);

        public struct Shake
        {
            public readonly float Magnitude;
            public readonly float Duration;
            public float Time;
            public readonly bool UnscaledTime;

            public Shake(float magnitude, float duration, bool unscaledTime = false)
            {
                Magnitude = magnitude;
                Duration = duration;
                Time = 0.0f;
                UnscaledTime = unscaledTime;
            }

            public float ComputeCurrentStrength() =>
                Magnitude * Mathf.Clamp01(1 - Time / Duration);
        }
    }
}