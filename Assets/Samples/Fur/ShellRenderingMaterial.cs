using UnityEngine;

namespace Samples.Fur
{
    [RequireComponent(typeof(MeshRenderer))]
    [ExecuteInEditMode]
    public class ShellRenderingMaterial : MonoBehaviour
    {
        [SerializeField] private Material _materialPreset;
        [SerializeField] [Min(0.0f)] private float _height = 1.0f;
        [SerializeField] [Min(1)] private int _layersCount = 10;

        private void Awake()
        {
            UpdateMaterials();
        }

        private void OnValidate()
        {
            UpdateMaterials();
        }

        private void UpdateMaterials()
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            var materials = new Material[_layersCount];
            for (int i = 0; i < _layersCount; i++)
            {
                materials[i] = _materialPreset;
            }

            meshRenderer.sharedMaterials = materials;

            var materialPropertyBlock = new MaterialPropertyBlock();

            for (int i = 0; i < _layersCount; i++)
            {
                float normalizedHeight = (float) i / (_layersCount - 1);
                materialPropertyBlock.SetFloat("_Height", _height * normalizedHeight);
                materialPropertyBlock.SetFloat("_NormalizedHeight", normalizedHeight);

                meshRenderer.SetPropertyBlock(materialPropertyBlock, i);
            }
        }
    }
}