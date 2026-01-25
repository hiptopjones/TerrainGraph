using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Windows;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class RangeNode
        : ExecutableNode<OptionValuesBase, RangeNode.InputValues, HeightGrid>
    {
        public class InputValues : InputValuesBase
        {
            public HeightGrid Grid;

            [ValidIf(nameof(IsValidFromRange))]
            public Vector2 FromRange;

            [ValidIf(nameof(IsValidToRange))]
            public Vector2 ToRange;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    Grid?.VersionHash, FromRange, ToRange
                );
            }
        }

        protected override void OnDefineInputPorts(ICustomInputPortDefinitionContext<InputValues> context)
        {
            context.BuildInputPort(x => x.Grid);

            context.AddInputPort(x => x.FromRange)
                .WithDefaultValue(new Vector2(0, 1))
                .Build();

            context.AddInputPort(x => x.ToRange)
                .WithDefaultValue(new Vector2(0, 1))
                .Build();
        }

        private bool IsValidFromRange(InputValues inputs, GraphLogger graphLogger)
        {
            var fromRangeDisplayName = NodeHelpers.GetDisplayName(typeof(InputValues), nameof(InputValues.FromRange));

            if (inputs.FromRange.x == inputs.FromRange.y)
            {
                graphLogger?.LogWarning($"{fromRangeDisplayName} value invalid (x != y)", this);
                inputs.FromRange = new Vector2(inputs.FromRange.x, inputs.FromRange.x + 0.00001f);
            }

            return true;
        }

        private bool IsValidToRange(InputValues inputs, GraphLogger graphLogger)
        {
            var toRangeDisplayName = NodeHelpers.GetDisplayName(typeof(InputValues), nameof(InputValues.ToRange));

            if (inputs.ToRange.x == inputs.ToRange.y)
            {
                graphLogger?.LogWarning($"{toRangeDisplayName} value invalid (x != y)", this);
                inputs.ToRange = new Vector2(inputs.ToRange.x, inputs.ToRange.x + 0.00001f);
            }

            return true;
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var inputGrid = Inputs.Grid;
                var fromRange = Inputs.FromRange;
                var toRange = Inputs.ToRange;

                var size = inputGrid.Size;

                var inputTexture = inputGrid.RenderTexture;
                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader($"Shaders/{nameof(RangeNode)}", out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_InTexture", inputTexture);
                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetFloat("_FromLow", fromRange.x);
                shader.SetFloat("_FromHigh", fromRange.y);
                shader.SetFloat("_ToLow", toRange.x);
                shader.SetFloat("_ToHigh", toRange.y);

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