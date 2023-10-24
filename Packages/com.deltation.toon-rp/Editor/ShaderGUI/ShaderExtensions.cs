using UnityEngine;

namespace DELTation.ToonRP.Editor.ShaderGUI
{
    internal static class ShaderExtensions
    {
        public static float GetPropertyDefaultFloatValueById(this Shader shader, int propertyId,
            float defaultValue = 0.0f)
        {
            int propertyIndex = GetPropertyIndex(shader, propertyId);
            return propertyIndex == -1 ? defaultValue : shader.GetPropertyDefaultFloatValue(propertyIndex);
        }

        private static int GetPropertyIndex(Shader shader, int propertyNameId)
        {
            int propertyCount = shader.GetPropertyCount();

            for (int i = 0; i < propertyCount; i++)
            {
                if (shader.GetPropertyNameId(i) == propertyNameId)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}