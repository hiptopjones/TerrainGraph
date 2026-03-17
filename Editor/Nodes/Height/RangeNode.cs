using System;
using UnityEngine;

namespace CodeFirst.TerrainGraph.Editor
{
    [Serializable]
    public class RangeNode
        : BaseNode<RangeNode.OptionValues, RangeNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            [Passthru]
            public HeightGrid Grid;

            [ValidIf(nameof(IsValidFromRange))]
            public Vector2 FromRange;

            [ValidIf(nameof(IsValidToRange))]
            public Vector2 ToRange;
        }

        protected override void OnDefineCustomInputPorts(IPortDefinitionContext context)
        {
            var classModel = ClassModelCache.GetClassModel<InputValues>();

            var fromModel = classModel.GetFieldModel(nameof(InputValues.FromRange));
            fromModel.DefaultValue = new Vector2(0, 1);

            var toModel = classModel.GetFieldModel(nameof(InputValues.ToRange));
            toModel.DefaultValue = new Vector2(0, 1);

            // Build the ports automatically
            base.OnDefineCustomInputPorts(context);
        }

        private ValidationResult IsValidFromRange(InputValues inputs)
        {
            var classModel = ClassModelCache.GetClassModel<InputValues>();
            var fromModel = classModel.GetFieldModel(nameof(InputValues.FromRange));

            if (inputs.FromRange.x == inputs.FromRange.y)
            {
                inputs.FromRange = new Vector2(inputs.FromRange.x, inputs.FromRange.x + 0.00001f);
                return ValidationResult.Warning($"{fromModel.DisplayName} input invalid (x != y)");
            }

            return ValidationResult.Ok();
        }

        private ValidationResult IsValidToRange(InputValues inputs)
        {
            var classModel = ClassModelCache.GetClassModel<InputValues>();
            var toModel = classModel.GetFieldModel(nameof(InputValues.ToRange));

            if (inputs.ToRange.x == inputs.ToRange.y)
            {
                inputs.ToRange = new Vector2(inputs.ToRange.x, inputs.ToRange.x + 0.00001f);
                return ValidationResult.Warning($"{toModel.DisplayName} input invalid (x != y)");
            }

            return ValidationResult.Ok();
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

                if (!ComputeHelpers.TryLoadComputeShader(nameof(RangeNode), out var shader))
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