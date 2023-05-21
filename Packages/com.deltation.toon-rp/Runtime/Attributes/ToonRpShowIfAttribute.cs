using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

namespace DELTation.ToonRP.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ToonRpShowIfAttribute : PropertyAttribute
    {
        public enum ShowIfMode
        {
            ShowField,
            ShowHelpBox,
        }

        public ToonRpShowIfAttribute([NotNull] string fieldName) =>
            FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));

        [CanBeNull]
        public string FieldName { get; }

        public ShowIfMode Mode { get; set; } = ShowIfMode.ShowField;
        public string HelpBoxMessage { get; set; }
        public HelpBoxMessageType HelpBoxMessageType { get; set; } = HelpBoxMessageType.Info;
    }
}