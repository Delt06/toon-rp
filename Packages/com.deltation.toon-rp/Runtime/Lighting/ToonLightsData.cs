using System.Collections.Generic;

namespace DELTation.ToonRP.Lighting
{
    public class ToonLightsData
    {
        public readonly List<int> AdditionalLightsIndices = new();

        public void Reset()
        {
            AdditionalLightsIndices.Clear();
        }
    }
}