using System;
using UnityEngine;
using UnityEngine.Splines;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ImportTextureNode
        : BaseNode<ImportTextureNode.OptionValues, ImportTextureNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            [NotNull]
            public Texture2D Texture;
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