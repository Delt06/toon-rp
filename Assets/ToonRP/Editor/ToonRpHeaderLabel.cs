using UnityEngine.UIElements;

namespace ToonRP.Editor
{
    /// <summary>
    /// Bold and underlined Label.
    /// </summary>
    public class ToonRpHeaderLabel : Label
    {
        public ToonRpHeaderLabel(string text) :
            base($"<u><b>{text}</b></u>") { }
    }
}