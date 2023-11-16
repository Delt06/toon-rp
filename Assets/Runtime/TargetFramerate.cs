using UnityEngine;

namespace Runtime
{
    public class TargetFramerate : MonoBehaviour
    {
        [SerializeField] private int _targetFramerate = 60;

        private void Awake()
        {
            Application.targetFrameRate = _targetFramerate;
        }
    }
}