using UnityEditor;
using UnityEngine.UIElements;

namespace Indiecat.TerrainGraph.Editor
{
    [CustomPropertyDrawer(typeof(NormalizedFloatParameter))]
    public class NormalizedFloatParameterDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty valueProp = property.FindPropertyRelative("Value");

            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;

            var slider = new Slider()
            {
                lowValue = 0,
                highValue = 1,
                bindingPath = valueProp.propertyPath
            };
            slider.style.flexGrow = 1;
            root.Add(slider);

            // TODO: Add range enforcement on this field
            var field = new FloatField()
            {
                bindingPath = valueProp.propertyPath
            };
            root.Add(field);

            return root;
        }
    }
}
