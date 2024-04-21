using UnityEngine;

namespace Samples.MotionBlur
{
    public class TimeScaleController : MonoBehaviour
    {
        [Min(0.0f)]
        public float TimeScale = 1.0f;

        private void Update()
        {
            Time.timeScale = TimeScale;
        }
    }
}