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
            var container = new VisualElement
            {
                style =
                {
                    flexGrow = 1
                }
            };

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
            container.Add(image);

            var label = new Label();
            container.Add(label);

            var target = fieldInfo.GetValue(property.serializedObject.targetObject) as PreviewImage;

            // Node list preview can have a null target
            if (target != null)
            {
                target.Containers.Add(container);
                target.UpdateTexture(target.Texture, target.GridSize);

                // Ensures that previews are populated when the graph is loaded into the editor
                container.schedule.Execute(() =>
                {
                    target.UpdateTexture(target.Texture, target.GridSize);
                }
                ).StartingIn(100);
            }

            return container;
        }
    }
}
