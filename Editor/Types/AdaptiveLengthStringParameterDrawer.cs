using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Indiecat.TerrainGraph.Editor
{
    [CustomPropertyDrawer(typeof(AdaptiveLengthStringParameter))]
    public class AdaptiveLengthStringParameterDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty valueProp = property.FindPropertyRelative("Value");

            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;

            var textField = new TextField()
            {
                bindingPath = valueProp.propertyPath
            };
            root.Add(textField);

            textField.RegisterValueChangedCallback(e =>
            {
                textField.style.minWidth = Mathf.Max(100, e.newValue.Length * 7);
            });

            return root;
        }
    }
}
