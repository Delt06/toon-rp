#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define SET_MATERIAL_NAME
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public class ToonPipelineMaterial : IDisposable
    {
        private readonly Func<Material> _materialFactory;
        [CanBeNull] private Material _material;

        private ToonPipelineMaterial(Func<Material> materialFactory, Shader shader,
            [CanBeNull] string shaderName = null)
        {
            _materialFactory = materialFactory;
            Shader = shader;
            ShaderName = shaderName ?? shader.name;
        }

        public ToonPipelineMaterial(string shaderPath, string materialName)
            : this(
                () => CreateEngineMaterial(shaderPath, materialName),
                Shader.Find(shaderPath), shaderPath
            ) { }

        public ToonPipelineMaterial(Shader shader, string materialName)
            : this(
                () => CreateEngineMaterial(shader, materialName),
                shader
            ) { }

        public ToonPipelineMaterial(Material materialTemplate, string materialName)
            : this(
                () => CreateEngineMaterial(materialTemplate, materialName),
                materialTemplate.shader
            ) { }

        public Shader Shader { get; }
        public string ShaderName { get; }

        public void Dispose()
        {
            if (_material != null)
            {
                CoreUtils.Destroy(_material);
                _material = null;
            }
        }

        public Material GetOrCreate()
        {
            if (_material == null)
            {
                _material = _materialFactory();
            }

            return _material;
        }

        [Conditional("SET_MATERIAL_NAME")]
        private static void SetMaterialName(Material material, string name)
        {
            material.name = name;
        }

        private static Material CreateEngineMaterial(Shader shader, string materialName)
        {
            Material material = CoreUtils.CreateEngineMaterial(shader);
            SetMaterialName(material, materialName);
            return material;
        }

        private static Material CreateEngineMaterial(string shaderPath, string materialName)
        {
            Material material = CoreUtils.CreateEngineMaterial(shaderPath);
            SetMaterialName(material, materialName);
            return material;
        }

        private static Material CreateEngineMaterial(Material source, string materialName)
        {
            var material = new Material(source)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            SetMaterialName(material, materialName);
            return material;
        }
    }
}