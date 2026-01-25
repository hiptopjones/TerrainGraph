using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Windows;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class BlendNode
        : ExecutableNode<BlendNode.OptionValues, BlendNode.InputValues, HeightGrid>
    {
        public enum BlendOperator
        {
            Add = 100,
            Subtract = 200,
            Multiply = 300,
            Divide = 400,
            Minimum = 500,
            Maximum = 600,
            Average = 700,
            Compare = 1000,
        }

        public class OptionValues : OptionValuesBase
        {
            [DefaultValue(BlendOperator.Multiply)]
            public BlendOperator BlendOperator;

            [DisplayName("Flip Inputs")]
            public bool IsFlipped;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    BlendOperator, IsFlipped
                );
            }
        }

        public class InputValues : InputValuesBase
        {
            [ValidIf(nameof(IsMatchingSize))]
            public HeightGrid Grid1;
            public HeightGrid Grid2;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    Grid1?.VersionHash, Grid2?.VersionHash
                );
            }
        }

        protected override void OnDefineInputPorts(ICustomInputPortDefinitionContext<InputValues> context)
        {
            if (Options.IsFlipped)
            {
                context.BuildInputPort(x => x.Grid2);
                context.BuildInputPort(x => x.Grid1);
            }
            else
            {
                context.BuildInputPort(x => x.Grid1);
                context.BuildInputPort(x => x.Grid2);
            }
        }

        private bool IsMatchingSize(InputValues inputs, GraphLogger graphLogger)
        {
            var grid1DisplayName = NodeHelpers.GetDisplayName(typeof(InputValues), nameof(InputValues.Grid1));
            var grid2DisplayName = NodeHelpers.GetDisplayName(typeof(InputValues), nameof(InputValues.Grid2));

            var isValid = true;

            if (isValid &&
                (inputs.Grid1.RenderTexture.width != inputs.Grid2.RenderTexture.width ||
                inputs.Grid1.RenderTexture.height != inputs.Grid2.RenderTexture.height))
            {
                graphLogger?.LogError($"{grid1DisplayName} and {grid2DisplayName} size mismatch", this);
                isValid = false;
            }

            return isValid;
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var blendOperator = Options.BlendOperator;
                var isFlipped = Options.IsFlipped;
                var inputGrid1 = Inputs.Grid1;
                var inputGrid2 = Inputs.Grid2;

                var size = inputGrid1.Size;

                var keywordBuilder = new KeywordBuilder();
                keywordBuilder.AddKeyword($"OP_{blendOperator.ToString().ToUpper()}");
                keywordBuilder.AddKeyword(isFlipped ? "ARGS_FLIPPED" : "ARGS_NORMAL");

                var inputTexture1 = inputGrid1.RenderTexture;
                var inputTexture2 = inputGrid2.RenderTexture;
                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader($"Shaders/{nameof(BlendNode)}", out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_InTexture1", inputTexture1);
                shader.SetTexture(kernel, "_InTexture2", inputTexture2);
                shader.SetTexture(kernel, "_OutTexture", outputTexture);

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