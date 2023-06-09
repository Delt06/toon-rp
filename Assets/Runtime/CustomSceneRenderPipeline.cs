using UnityEngine;
using UnityEngine.Rendering;

namespace Runtime
{
    [ExecuteAlways]
    public sealed class CustomSceneRenderPipeline : MonoBehaviour
    {
        [SerializeField] private RenderPipelineAsset _pipelineAsset;

        private void OnEnable()
        {
            SetPipeline();
        }

        private void OnValidate()
        {
            SetPipeline();
        }

        private void SetPipeline()
        {
            if (_pipelineAsset != null)
            {
                GraphicsSettings.renderPipelineAsset = _pipelineAsset;
            }
        }
    }
}