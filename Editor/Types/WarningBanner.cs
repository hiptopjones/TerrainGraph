using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class WarningBanner
    {
        // NOTE: Do not write directly to this field, use UpdateProperties()
        public string Text;

        public List<VisualElement> Containers = new();

        public void UpdateProperties(string text)
        {
            Text = text;

            foreach (var container in Containers)
            {
                var label = container.Q<Label>();
                if (label != null)
                {
                    label.text = text;
                    label.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
                }
            }
        }
    }
}
