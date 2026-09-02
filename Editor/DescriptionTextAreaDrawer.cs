using UnityEditor;
using UnityEngine;
using Xprees.Core.DescriptionAttribute;

namespace Xprees.Core.Editor
{
    /// Draws a string field as a foldout: a single-line preview when collapsed,
    /// and a full-text area when expanded.
    [CustomPropertyDrawer(typeof(DescriptionTextAreaAttribute))]
    public class DescriptionTextAreaDrawer : PropertyDrawer
    {
        // Tweak this to change the font size of both the expanded text area and the collapsed preview.
        private const int textFontSize = 12;

        // Separator line drawn below the field, separating it from the next one in the Inspector.
        private const float separatorHeight = 1f;
        private const float separatorSpacing = 4f;
        private readonly static Color separatorColor = new(0.5f, 0.5f, 0.5f, 0.5f);

        private static float LineHeight => EditorGUIUtility.singleLineHeight;
        private static float SeparatorTotalHeight => separatorSpacing + separatorHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            var foldoutRect = new Rect(position.x, position.y, position.width, LineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            var contentRect = new Rect(position.x, foldoutRect.yMax, position.width,
                position.height - LineHeight - SeparatorTotalHeight);
            if (property.isExpanded)
            {
                var textAreaStyle = new GUIStyle(EditorStyles.textArea) { fontSize = textFontSize };

                EditorGUI.BeginChangeCheck();
                var newValue = EditorGUI.TextArea(contentRect, property.stringValue, textAreaStyle);
                if (EditorGUI.EndChangeCheck())
                {
                    property.stringValue = newValue;
                }
            }
            else if (!string.IsNullOrEmpty(property.stringValue))
            {
                var previewStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel) { fontSize = textFontSize };
                EditorGUI.LabelField(contentRect, GetPreview(property.stringValue), previewStyle);
            }

            var separatorRect = new Rect(position.x, position.yMax - separatorHeight, position.width, separatorHeight);
            EditorGUI.DrawRect(separatorRect, separatorColor);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            if (!property.isExpanded)
            {
                var previewLines = string.IsNullOrEmpty(property.stringValue) ? 1 : 2;
                return previewLines * LineHeight + SeparatorTotalHeight;
            }

            var textAreaAttribute = (DescriptionTextAreaAttribute) attribute;
            var lineCount = string.IsNullOrEmpty(property.stringValue) ? 1 : property.stringValue.Split('\n').Length;
            lineCount = Mathf.Clamp(lineCount + 1, textAreaAttribute.expandedMinLines, textAreaAttribute.expandedMaxLines);
            return LineHeight + lineCount * LineHeight + SeparatorTotalHeight;
        }

        private static string GetPreview(string value)
        {
            var firstLine = value.Split('\n')[0];
            return firstLine.Length > 100 ? firstLine[..100] + "…" : firstLine;
        }
    }
}