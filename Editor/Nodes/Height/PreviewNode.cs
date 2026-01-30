using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class PreviewNode
        : BaseNode<PreviewNode.OptionValues, PreviewNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            [Passthru]
            public HeightGrid Grid;
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var inputGrid = Inputs.Grid;

                var size = inputGrid.Size;

                var inputTexture = inputGrid.RenderTexture;
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
