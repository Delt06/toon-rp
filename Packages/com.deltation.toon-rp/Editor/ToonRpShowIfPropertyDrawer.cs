using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DELTation.ToonRP.Attributes;
using UnityEditor;
using UnityEngine.UIElements;

namespace DELTation.ToonRP.Editor
{
    [CustomPropertyDrawer(typeof(ToonRpShowIfAttribute))]
    public class ToonRpShowIfPropertyDrawer : ToonRpEditorDecorator
    {
        private const string PathSeparator = ".";

        private const BindingFlags MemberBindingFlags = BindingFlags.Instance | BindingFlags.Static |
                                                        BindingFlags.Public | BindingFlags.NonPublic;

        private readonly Dictionary<SerializedProperty, HelpBox> _helpBoxes = new();

        private static IEnumerable<string> BuildPropertyPathPieces(string basePath, int lastSkips,
            params string[] appends)
        {
            string[] propertyPathPieces = basePath.Split(PathSeparator);
            return propertyPathPieces.SkipLast(lastSkips).Concat(appends);
        }

        private static string BuildPropertyPath(string basePath, int lastSkips, params string[] appends) =>
            string.Join(PathSeparator, BuildPropertyPathPieces(basePath, lastSkips, appends));

        private static object GetFieldValueRecursive(object currentObject, IEnumerable<string> pathPieces)
        {
            foreach (string fieldName in pathPieces)
            {
                if (fieldName == "Array")
                {
                    continue;
                }

                if (fieldName.StartsWith("data["))
                {
                    Match match = Regex.Match(fieldName, @"^data\[([0-9]*)\]$");
                    if (!match.Success)
                    {
                        throw new ArgumentException($"Invalid data field name: {fieldName}.", nameof(pathPieces));
                    }

                    int index = int.Parse(match.Groups[1].Value);

                    if (currentObject is IList list)
                    {
                        currentObject = list[index];
                    }
                    else
                    {
                        throw new ArgumentException($"Current object is not a list: {currentObject.GetType()}.");
                    }
                }
                else
                {
                    Type type = currentObject.GetType();
                    FieldInfo field = type.GetField(fieldName, MemberBindingFlags);
                    if (field == null)
                    {
                        throw new ArgumentException($"Could not find field {fieldName} in {type}.", nameof(pathPieces));
                    }

                    currentObject = field.GetValue(currentObject);
                }
            }

            return currentObject;
        }

        protected override void BeforeProperty(VisualElement root, SerializedProperty property)
        {
            base.BeforeProperty(root, property);
            var showIfAttribute = (ToonRpShowIfAttribute) attribute;
            if (showIfAttribute.Mode != ToonRpShowIfAttribute.ShowIfMode.ShowHelpBox)
            {
                return;
            }

            var helpBox = new HelpBox(showIfAttribute.HelpBoxMessage, showIfAttribute.HelpBoxMessageType);
            _helpBoxes[property] = helpBox;
            root.Add(helpBox);
        }

        protected override VisualElement AddProperty(VisualElement root, SerializedProperty property)
        {
            var showIfAttribute = (ToonRpShowIfAttribute) attribute;
            string basePropertyPath = property.propertyPath;

            string conditionPath = BuildPropertyPath(basePropertyPath, 1, showIfAttribute.FieldName);
            Func<bool?> getPropertyValue = null;

            {
                if (showIfAttribute.FieldName != null)
                {
                    IEnumerable<string> propertyPathPieces = BuildPropertyPathPieces(basePropertyPath, 1);

                    getPropertyValue = () =>
                    {
                        object baseObject =
                            GetFieldValueRecursive(property.serializedObject.targetObject, propertyPathPieces);
                        Type type = baseObject.GetType();
                        PropertyInfo propertyInfo = type.GetProperty(showIfAttribute.FieldName, MemberBindingFlags);
                        return propertyInfo?.PropertyType == typeof(bool)
                            ? (bool) propertyInfo.GetValue(baseObject)
                            : null;
                    };
                    if (getPropertyValue() == null)
                    {
                        getPropertyValue = null;
                    }
                }
            }


            var imguiContainer = new IMGUIContainer();

            root.Add(imguiContainer);
            VisualElement visualElement = base.AddProperty(root, property);
            imguiContainer.onGUIHandler = () =>
            {
                if (showIfAttribute.FieldName == null)
                {
                    EditorGUILayout.HelpBox("Field name is null.", MessageType.Error);
                    return;
                }

                SerializedProperty conditionProperty = property.serializedObject.FindProperty(conditionPath);
                if (conditionProperty == null)
                {
                    if (getPropertyValue != null)
                    {
                        bool? propertyValue = getPropertyValue();
                        if (propertyValue.HasValue)
                        {
                            SetTargetVisible(visualElement, property, propertyValue.Value);
                        }

                        return;
                    }

                    EditorGUILayout.HelpBox($"Could not find the property {showIfAttribute.FieldName}",
                        MessageType.Error
                    );
                    return;
                }

                if (conditionProperty.type != "bool")
                {
                    EditorGUILayout.HelpBox($"{showIfAttribute.FieldName} is not of type bool.", MessageType.Error);
                    return;
                }

                SetTargetVisible(visualElement, property, conditionProperty.boolValue);
            };

            return visualElement;
        }

        private void SetTargetVisible(VisualElement visualElement, SerializedProperty property, bool visible)
        {
            VisualElement target = _helpBoxes.TryGetValue(property, out HelpBox helpBox) ? helpBox : visualElement;
            target.SetVisible(visible);
        }
    }
}