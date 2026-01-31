using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class PowerNode
        : BaseNode<PowerNode.OptionValues, PowerNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
            [DisplayName("Ignore Zero")]
            public bool IsZeroIgnored;
        }

        public class InputValues : InputValuesBase
        {
            [Passthru]
            public HeightGrid Grid;

            [DefaultValue(0.5f)]
            public float Value;
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var arithmeticOperator = ArithmeticNode.ArithmeticOperator.Power;
                var isZeroIgnored = Options.IsZeroIgnored;
                var isFlipped = false;
                var inputGrid = Inputs.Grid;
                var value = Inputs.Value;

                var size = inputGrid.Size;

                var inputTexture = inputGrid.RenderTexture;
                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ShaderWrappers.TryArithmeticOperation(
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