using System;
using UnityEngine;

namespace DELTation.ToonRP.Extensions.BuildIn
{
    [CreateAssetMenu(menuName = Path + "Test")]
    public class ToonTestRenderingExtensionAsset : ToonRenderingExtensionAsset
    {
        [SerializeField] private ToonRenderingEvent _event;

        public override ToonRenderingEvent Event => _event;

        public override IToonRenderingExtension CreateExtension() => new ToonTestRenderingExtension(_event);

        protected override string[] ForceIncludedShaderNames() => Array.Empty<string>();
    }
}