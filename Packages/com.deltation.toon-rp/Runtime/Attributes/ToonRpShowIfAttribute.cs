using System;
using JetBrains.Annotations;
using UnityEngine;

namespace DELTation.ToonRP.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ToonRpShowIfAttribute : PropertyAttribute
    {
        public ToonRpShowIfAttribute([NotNull] string fieldName) =>
            FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));

        [CanBeNull]
        public string FieldName { get; }
    }
}