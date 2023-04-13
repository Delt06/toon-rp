using System;
using JetBrains.Annotations;
using UnityEngine;

namespace DELTation.ToonRP
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ToonRpHeaderAttribute : PropertyAttribute
    {
        public ToonRpHeaderAttribute([NotNull] string text) =>
            Text = text ?? throw new ArgumentNullException(nameof(text));

        public ToonRpHeaderAttribute() { }

        [CanBeNull]
        public string Text { get; set; }
    }
}