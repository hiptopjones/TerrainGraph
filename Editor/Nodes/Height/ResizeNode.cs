using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ResizeNode
        : BaseNode<ResizeNode.OptionValues, ResizeNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
            public override int GetHashCode()
            {
                // Avoid using the base hash code
                return 0;
            }
        }

        public class InputValues : InputValuesBase
        {
            [Passthru]
            public HeightGrid Grid;

            [MinValue(16), DefaultValue(256)]
            public int Size;

            [DefaultValue(true)]

            public bool PreserveScale;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    Grid?.VersionHash, Size, PreserveScale
                );
            }
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var inputGrid = Inputs.Grid;
                var targetSize = Inputs.Size;
                var preserveScale = Inputs.PreserveScale;

                var sourceSize = inputGrid.Size;
                var scale = preserveScale ? sourceSize / (float)targetSize : 1;

                var inputTexture = inputGrid.RenderTexture;
                var outputTexture = GetOrCreateNodeRenderTexture(targetSize);

                if (!ComputeHelpers.TryLoadComputeShader(nameof(ResizeNode), out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_InTexture", inputTexture);
                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetFloat("_Scale", scale);

                var groups = Mathf.CeilToInt(targetSize / 8.0f);
                shader.Dispatch(kernel, groups, groups, 1);

                var outputGrid = new HeightGrid(targetSize);

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