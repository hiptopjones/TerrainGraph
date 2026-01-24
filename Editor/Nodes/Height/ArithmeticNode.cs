using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ArithmeticNode
        : ExecutableNode<ArithmeticNode.OptionValues, ArithmeticNode.InputValues, HeightGrid>
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

            [DisplayName("Flip Inputs")]
            public bool IsFlipped;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    ArithmeticOperator, IsFlipped
                );
            }
        }

        public class InputValues : InputValuesBase
        {
            public HeightGrid Grid;

            [DefaultValue(0.5f)]
            public float Value;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    Grid?.VersionHash, Value
                );
            }
        }

        protected override void OnDefineInputPorts(ICustomInputPortDefinitionContext<InputValues> context)
        {
            if (Options.IsFlipped)
            {
                context.BuildInputPort(x => x.Value);
                context.BuildInputPort(x => x.Grid);
            }
            else
            {
                context.BuildInputPort(x => x.Grid);
                context.BuildInputPort(x => x.Value);
            }
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var arithmeticOperator = Options.ArithmeticOperator;
                var isFlipped = Options.IsFlipped;
                var inputGrid = Inputs.Grid;
                var value = Inputs.Value;

                var size = inputGrid.Size;

                var keywordBuilder = new KeywordBuilder();
                keywordBuilder.AddKeyword($"OP_{arithmeticOperator.ToString().ToUpper()}");
                keywordBuilder.AddKeyword(isFlipped ? "ARGS_FLIPPED" : "ARGS_NORMAL");

                var inputTexture = inputGrid.RenderTexture;
                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader($"Shaders/{nameof(ArithmeticNode)}", out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_InTexture", inputTexture);
                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetFloat("_Value", value);

                shader.shaderKeywords = keywordBuilder.GetKeywords();

                var groups = Mathf.CeilToInt(size / 8.0f);
                shader.Dispatch(kernel, groups, groups, 1);

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