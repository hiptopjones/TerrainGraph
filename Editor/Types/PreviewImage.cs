using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CodeFirst.TerrainGraph.Editor
{
    [Serializable]
    public class PreviewImage
    {
        // NOTE: Do not write directly to this field, use UpdateTexture()
        public Texture Texture;
        public int GridSize;

        public List<VisualElement> Containers = new();

        public void UpdateTexture(Texture texture, int gridSize)
        {
            Texture = texture;
            GridSize = gridSize;

            foreach (var container in Containers)
            {
                var image = container.Q<Image>();
                if (image != null)
                {
                    image.image = texture;
                }

                var label = container.Q<Label>();
                if (label != null)
                {
                    label.text = $"{GridSize} x {GridSize}";
                }
            }
        }
    }
}
