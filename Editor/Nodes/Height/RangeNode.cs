using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class RangeNode
        : ExecutableNode<OptionValuesBase, RangeNode.InputValues, HeightGrid>
    {
        public class InputValues : InputValuesBase
        {
            public HeightGrid Grid;
            public Vector2 FromRange;
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