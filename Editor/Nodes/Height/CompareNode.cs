using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class CompareNode
        : BaseNode<CompareNode.OptionValues, CompareNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
            [DisplayName("Ignore Zero")]
            public bool IsZeroIgnored;

            [DisplayName("Flip Inputs")]
            public bool IsFlipped;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    IsZeroIgnored, IsFlipped
                );
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
                var arithmeticOperator = ArithmeticNode.ArithmeticOperator.Compare;
                var isZeroIgnored = Options.IsZeroIgnored;
                var isFlipped = Options.IsFlipped;
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