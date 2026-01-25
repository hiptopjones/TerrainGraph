using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class TextureHeightNode
        : BaseNode<OptionValuesBase, TextureHeightNode.InputValues, HeightGrid>
    {
        public class InputValues : InputValuesBase
        {
            public Texture2D Texture;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    Texture
                );
            }
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var inputTexture = Inputs.Texture;

                var size = Mathf.Max(inputTexture.width, inputTexture.height);

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                Graphics.Blit(inputTexture, outputTexture);

                var outputGrid = new HeightGrid(size);

                outputGrid.RenderTexture = outputTexture;
                outputGrid.VersionHash = Inputs.VersionHash;

                CacheData.Output = outputGrid;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }
    }
}