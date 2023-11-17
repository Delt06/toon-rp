using UnityEngine;

namespace DELTation.ToonRP
{
    public readonly struct ToonClearValue
    {
        public readonly bool ClearColor;
        public readonly bool ClearDepth;
        public readonly Color BackgroundColor;

        public ToonClearValue(bool clearColor, bool clearDepth, Color backgroundColor)
        {
            ClearDepth = clearDepth;
            ClearColor = clearColor;
            BackgroundColor = backgroundColor;
        }
    }
}