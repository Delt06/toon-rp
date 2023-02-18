using UnityEngine;
using UnityEngine.Rendering;

namespace ToonRP.Runtime
{
	public sealed class ToonLighting
	{
		private const string BufferName = "Lighting";
		private static readonly int DirectionalLightColorId = Shader.PropertyToID("_DirectionalLightColor");
		private static readonly int DirectionalLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");

		private readonly CommandBuffer _buffer = new() { name = BufferName };

		public void Setup(ScriptableRenderContext context)
		{
			_buffer.BeginSample(BufferName);
			SetupDirectionalLight();
			_buffer.EndSample(BufferName);
			context.ExecuteCommandBuffer(_buffer);
			_buffer.Clear();
		}

		private void SetupDirectionalLight()
		{
			Light light = RenderSettings.sun;
			if (light != null)
			{
				_buffer.SetGlobalVector(DirectionalLightColorId, light.color.linear * light.intensity);
				_buffer.SetGlobalVector(DirectionalLightDirectionId, -light.transform.forward);
			}
			else
			{
				_buffer.SetGlobalVector(DirectionalLightColorId, Vector4.zero);
				_buffer.SetGlobalVector(DirectionalLightDirectionId, Vector4.zero);
			}
		}
	}
}