using UnityEngine;

namespace DELTation.ToonRP.Extensions.BuildIn
{
    public class ToonTestRenderingExtension : ToonRenderingExtensionBase
    {
        private readonly ToonRenderingEvent _event;
        public ToonTestRenderingExtension(ToonRenderingEvent @event) => _event = @event;

        public override void Render(in ToonRenderingExtensionContext context)
        {
            Debug.Log("Hello " + _event);
        }
    }
}