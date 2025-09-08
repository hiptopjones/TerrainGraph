using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(PreviewImage))]
public class PreviewImageDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var image = new Image
        {
            scaleMode = ScaleMode.ScaleToFit,
            style =
            {   
                flexGrow = 1,
                width = 200,
                height = 200,
                borderTopWidth = 1,
                borderBottomWidth = 1,
                borderLeftWidth = 1,
                borderRightWidth = 1,
                borderTopColor = new Color(0,0,0,0.25f),
                borderBottomColor = new Color(0,0,0,0.25f),
                borderLeftColor = new Color(0,0,0,0.25f),
                borderRightColor = new Color(0,0,0,0.25f),
                marginBottom = 6
            }
        };

        object target = property.serializedObject.targetObject;

        var wrapper = fieldInfo.GetValue(target) as PreviewImage;
        if (wrapper != null)
        {
            image.style.backgroundImage = wrapper.Texture;
        }

        return image;
    }
}
