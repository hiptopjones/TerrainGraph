using UnityEditor;
using UnityEngine.UIElements;

namespace Indiecat.TerrainGraph.Editor
{
    [CustomPropertyDrawer(typeof(RangedFloatParameter))]
    public class RangedFloatParameterDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty valueProp = property.FindPropertyRelative("Value");

            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;

            var slider = new Slider()
            {
                bindingPath = valueProp.propertyPath
            };
            slider.style.flexGrow = 1;
            container.Add(slider);

            // TODO: Add range enforcement on this field
            var field = new FloatField()
            {
                bindingPath = valueProp.propertyPath
            };
            container.Add(field);

            var target = fieldInfo.GetValue(property.serializedObject.targetObject) as RangedFloatParameter;

            if (target != null)
            {
                // Can happen due to serialization?
                if (target.Containers == null)
                {
                    target.Containers = new();
                }

                target.Containers.Add(container);
                target.UpdateRange(target.Min, target.Max);

                // Ensures that this UI is updated when the graph is loaded into the editor
                container.schedule.Execute(() =>
                {
                    target.UpdateRange(target.Min, target.Max);
                }
                ).StartingIn(100);
            }

            return container;
        }
    }
}
