using System;
using JetBrains.Annotations;
using UnityEngine;

namespace ToonRP.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ToonRpHeaderAttribute : PropertyAttribute
    {
        public ToonRpHeaderAttribute([NotNull] string text) =>
            Text = text ?? throw new ArgumentNullException(nameof(text));

        [CanBeNull]
        public string Text { get; set; }
    }
}