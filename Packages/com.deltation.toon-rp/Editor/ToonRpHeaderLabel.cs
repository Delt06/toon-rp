using DELTation.ToonRP.Attributes;
using UnityEngine.UIElements;

namespace DELTation.ToonRP.Editor
{
    /// <summary>
    ///     Bold and underlined Label.
    /// </summary>
    public class ToonRpHeaderLabel : Label
    {
        public ToonRpHeaderLabel(string text, float size = ToonRpHeaderAttribute.DefaultSize) :
            base($"<u><b>{text}</b></u>")
        {
            StyleLength fontSize = style.fontSize;
            fontSize.value = new Length(size, LengthUnit.Pixel);
            style.fontSize = fontSize;
        }
    }
}