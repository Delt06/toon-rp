using DELTation.ToonRP.Attributes;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine.UIElements;

namespace DELTation.ToonRP.Editor
{
    [CustomPropertyDrawer(typeof(ToonRpShowIfAttribute))]
    public class ToonRpShowIfPropertyDrawer : ToonRpEditorDecorator
    {
        [CanBeNull]
        private string _conditionPath;

        protected override VisualElement AddProperty(VisualElement root, SerializedProperty property)
        {
            var showIfAttribute = (ToonRpShowIfAttribute) attribute;
            string[] pathPieces = property.propertyPath.Split(".");
            pathPieces[^1] = showIfAttribute.FieldName;
            _conditionPath = string.Join(".", pathPieces);

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
                    EditorGUILayout.HelpBox($"Could not find a property at {_conditionPath}", MessageType.Error);
                    return;
                }

                if (conditionProperty.type != "bool")
                {
                    EditorGUILayout.HelpBox($"{showIfAttribute.FieldName} is not of type bool.", MessageType.Error);
                    return;
                }

                visualElement.SetVisible(conditionProperty.boolValue);
            };

            return visualElement;
        }
    }
}