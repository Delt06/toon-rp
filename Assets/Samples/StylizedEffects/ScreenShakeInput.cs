using DELTation.ToonRP;
using DELTation.ToonRP.PostProcessing.BuiltIn;
using UnityEngine;

namespace Samples.StylizedEffects
{
    public class ScreenShakeInput : MonoBehaviour
    {
        public float Magnitude = 0.1f;
        public float Duration = 0.5f;
        public ToonAdditionalCameraData CameraData;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ToonScreenSpaceShakePersistentData persistentData =
                    CameraData.GetPersistentData<ToonScreenSpaceShakePersistentData>();
                var shake = new ToonScreenSpaceShakePersistentData.Shake(Magnitude, Duration);
                persistentData.AddShake(shake);
            }
        }
    }
}