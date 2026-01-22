using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Indiecat.TerrainGraph.Editor
{
    [CustomPropertyDrawer(typeof(WarningBanner))]
    public class WarningBannerDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    backgroundColor = Color.yellow
                }
            };

            var label = new Label
            {
                style =
                {
                    paddingLeft = 5,
                    paddingTop = 5,
                    paddingRight = 5,
                    paddingBottom = 5,
                    fontSize = 50,
                    color = Color.black,
                    alignSelf = Align.Center
                }
            };
            container.Add(label);

            var target = fieldInfo.GetValue(property.serializedObject.targetObject) as WarningBanner;

            // Node list preview can have a null target
            if (target != null)
            {
                target.Containers.Add(container);
                target.UpdateProperties(target.Text);

                // Ensures that this UI is updated when the graph is loaded into the editor
                container.schedule.Execute(() =>
                {
                    target.UpdateProperties(target.Text);
                }
                ).StartingIn(100);
            }

            return container;
        }
    }
}
