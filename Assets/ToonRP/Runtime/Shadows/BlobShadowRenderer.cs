using UnityEngine;

namespace ToonRP.Runtime.Shadows
{
    [ExecuteAlways]
    public sealed class BlobShadowRenderer : MonoBehaviour
    {
        [SerializeField] [Min(0f)] private float _radius = 0.5f;

        public float Radius => _radius;

        private void OnEnable()
        {
            BlobShadowsManager.OnRendererEnabled(this);
        }

        private void OnDisable()
        {
            BlobShadowsManager.OnRendererDisabled(this);
        }
    }
}