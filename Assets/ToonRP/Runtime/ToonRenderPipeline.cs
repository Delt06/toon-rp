using UnityEngine;
using UnityEngine.Rendering;

namespace ToonRP.Runtime
{
	public sealed class ToonRenderPipeline : RenderPipeline
	{
		private readonly ToonCameraRenderer _cameraRenderer = new();
		private readonly ToonCameraRendererSettings _cameraRendererSettings;
		private readonly ToonRampSettings _globalRampSettings;

		public ToonRenderPipeline(in ToonCameraRendererSettings cameraRendererSettings,
			ToonRampSettings globalRampSettings)
		{
			_cameraRendererSettings = cameraRendererSettings;
			_globalRampSettings = globalRampSettings;
			GraphicsSettings.useScriptableRenderPipelineBatching = _cameraRendererSettings.UseSrpBatching;
		}

		protected override void Render(ScriptableRenderContext context, Camera[] cameras)
		{
			foreach (Camera camera in cameras)
			{
				_cameraRenderer.Render(context, camera, _cameraRendererSettings, _globalRampSettings);
			}
		}
	}
}