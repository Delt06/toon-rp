using System.Collections.Generic;

namespace DELTation.ToonRP.Lighting
{
    public class ToonLightsData
    {
        public readonly List<AdditionalLight> AdditionalLights = new();

        public void Reset()
        {
            AdditionalLights.Clear();
        }

        public struct AdditionalLight
        {
            public int VisibleLightIndex;
            public int? ShadowLightIndex;
        }
    }
}