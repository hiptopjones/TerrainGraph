using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ArithmeticNode
        : BaseNode<ArithmeticNode.OptionValues, ArithmeticNode.InputValues, HeightGrid>
    {
        public enum ArithmeticOperator
        {
            Add = 100,
            Subtract = 200,
            Multiply = 300,
            Divide = 400,
            Minimum = 500,
            Maximum = 600,
            Average = 700,
            Compare = 1000,
            Power = 2000,
        }

        public class OptionValues : OptionValuesBase
        {
            [DefaultValue(ArithmeticOperator.Multiply)]
            [DisplayName("Operation")]
            public ArithmeticOperator ArithmeticOperator;

            [DisplayName("Ignore Zero")]
            public bool IsZeroIgnored;

            [DisplayName("Flip Inputs")]
            public bool IsFlipped;
        }

        public class InputValues : InputValuesBase
        {
            [Passthru]
            public HeightGrid Grid;

            [DefaultValue(0.5f)]
            public float Value;
        }

        protected override void OnDefineCustomInputPorts(IPortDefinitionContext context)
        {
            if (Options.IsFlipped)
            {
                BuildInputPort(context, x => x.Value);
                BuildInputPort(context, x => x.Grid);
            }
            else
            {
                BuildInputPort(context, x => x.Grid);
                BuildInputPort(context, x => x.Value);
            }
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var arithmeticOperator = Options.ArithmeticOperator;
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