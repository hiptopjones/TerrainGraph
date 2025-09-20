using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class PreviewImage
    {
        // NOTE: Do not write directly to this field, use UpdateTexture()
        public Texture2D Texture;

        public List<Image> Images = new();

        public void UpdateTexture(Texture2D texture)
        {
            Texture = texture;

            foreach (var image in Images)
            {
                if (image != null)
                {
                    image.image = texture;
                }
            }
        }
    }
}
