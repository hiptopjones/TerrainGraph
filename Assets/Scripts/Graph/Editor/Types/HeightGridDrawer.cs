using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(HeightGrid))]
public class HeightGridDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    { 
        // Create root container
        var container = new VisualElement();

        // Get properties
        var widthProp = property.FindPropertyRelative(nameof(HeightGrid.Width));
        var heightProp = property.FindPropertyRelative(nameof(HeightGrid.Height));

        // Build display string
        string info = $"Height Grid: {widthProp.intValue} x {heightProp.intValue}";

        // Just show a label, no editable fields
        var label = new Label(info);

        // Optional: style the label
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.marginLeft = 4;

        container.Add(label);

        return container;
    }

}
