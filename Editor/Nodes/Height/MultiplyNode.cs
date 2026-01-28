using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class MultiplyNode
        : BaseNode<MultiplyNode.OptionValues, MultiplyNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
            public override int GetHashCode()
            {
                return 0;
            }
        }

        public class InputValues : InputValuesBase
        {
            [Passthru]
            public HeightGrid Grid;

            [DefaultValue(0.5f)]
            public float Value;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    Grid?.VersionHash, Value
                );
            }
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var arithmeticOperator = ArithmeticNode.ArithmeticOperator.Multiply;
                var isZeroIgnored = false;
                var isFlipped = false;
                var inputGrid = Inputs.Grid;
                var value = Inputs.Value;

                var size = inputGrid.Size;

                var inputTexture = inputGrid.RenderTexture;
                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ShaderWrappers.TryArithmetic(
                    inputTexture, value, arithmeticOperator, isZeroIgnored, isFlipped, size, ref outputTexture))
                {
                    return false;
                }

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