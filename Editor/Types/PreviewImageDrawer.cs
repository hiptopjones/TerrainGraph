using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Indiecat.TerrainGraph.Editor
{
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

            var target = fieldInfo.GetValue(property.serializedObject.targetObject) as PreviewImage;

            target.Images.Add(image);
            target.UpdateTexture(target.Texture);

            // Ensures that previews are populated when the graph is loaded into the editor
            image.schedule.Execute(() =>
            {
                target.UpdateTexture(target.Texture);
            }
            ).StartingIn(100);

            return image;
        }
    }
}
