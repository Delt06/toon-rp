using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DELTation.ToonRP.Attributes;
using JetBrains.Annotations;
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
        [CanBeNull]
        private string _conditionPath;
        [CanBeNull]
        private Func<bool?> _getPropertyValue;
        [CanBeNull]
        private HelpBox _helpBox;

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
                Type type = currentObject.GetType();
                FieldInfo field = type.GetField(fieldName, MemberBindingFlags);
                if (field == null)
                {
                    throw new ArgumentException($"Could not find field {fieldName} in {type}.", nameof(pathPieces));
                }

                currentObject = field.GetValue(currentObject);
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

            _helpBox = new HelpBox(showIfAttribute.HelpBoxMessage, showIfAttribute.HelpBoxMessageType);
            root.Add(_helpBox);
        }

        protected override VisualElement AddProperty(VisualElement root, SerializedProperty property)
        {
            var showIfAttribute = (ToonRpShowIfAttribute) attribute;
            string basePropertyPath = property.propertyPath;

            {
                _conditionPath = BuildPropertyPath(basePropertyPath, 1, showIfAttribute.FieldName);
            }

            {
                if (showIfAttribute.FieldName != null)
                {
                    IEnumerable<string> propertyPathPieces = BuildPropertyPathPieces(basePropertyPath, 1);

                    _getPropertyValue = () =>
                    {
                        object baseObject =
                            GetFieldValueRecursive(property.serializedObject.targetObject, propertyPathPieces);
                        Type type = baseObject.GetType();
                        PropertyInfo propertyInfo = type.GetProperty(showIfAttribute.FieldName, MemberBindingFlags);
                        return propertyInfo?.PropertyType == typeof(bool)
                            ? (bool) propertyInfo.GetValue(baseObject)
                            : null;
                    };
                    if (_getPropertyValue() == null)
                    {
                        _getPropertyValue = null;
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

                SerializedProperty conditionProperty = property.serializedObject.FindProperty(_conditionPath);
                if (conditionProperty == null)
                {
                    if (_getPropertyValue != null)
                    {
                        bool? propertyValue = _getPropertyValue();
                        if (propertyValue.HasValue)
                        {
                            SetTargetVisible(visualElement, propertyValue.Value);
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

                SetTargetVisible(visualElement, conditionProperty.boolValue);
            };

            return visualElement;
        }

        private void SetTargetVisible(VisualElement visualElement, bool visible)
        {
            VisualElement target = _helpBox ?? visualElement;
            target.SetVisible(visible);
        }
    }
}