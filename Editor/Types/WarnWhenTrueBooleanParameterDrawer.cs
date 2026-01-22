using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Indiecat.TerrainGraph.Editor
{
    [CustomPropertyDrawer(typeof(WarnWhenTrueBooleanParameter))]
    public class WarnWhenTrueBooleanParameterDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty valueProp = property.FindPropertyRelative("Value");

            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;

            var toggle = new Toggle()
            {
                bindingPath = valueProp.propertyPath
            };
            root.Add(toggle);

            toggle.RegisterValueChangedCallback(e =>
            {
                if (e.newValue)
                {
                    root.style.backgroundColor = Color.red;
                }
                else
                {
                    root.style.backgroundColor = StyleKeyword.Auto;
                }
            });

            return root;
        }
    }
}
