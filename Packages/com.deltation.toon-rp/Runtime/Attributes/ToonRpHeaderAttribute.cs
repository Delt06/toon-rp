using System;
using JetBrains.Annotations;
using UnityEngine;

namespace DELTation.ToonRP.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ToonRpHeaderAttribute : PropertyAttribute
    {
        public const int DefaultSize = 14;

        public ToonRpHeaderAttribute([NotNull] string text) =>
            Text = text ?? throw new ArgumentNullException(nameof(text));

        public ToonRpHeaderAttribute() { }

        [CanBeNull]
        public string Text { get; set; }

        public float Size { get; set; } = DefaultSize;
    }
}