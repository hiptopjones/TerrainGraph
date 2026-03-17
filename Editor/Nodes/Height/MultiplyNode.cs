using System;
using UnityEngine;

namespace CodeFirst.TerrainGraph.Editor
{
    [Serializable]
    public class MultiplyNode
        : BaseNode<MultiplyNode.OptionValues, MultiplyNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
            [DisplayName("Use Constant")]
            public bool UseConstantOperand;
        }

        public class InputValues : InputValuesBase
        {
            [Passthru]
            public HeightGrid Grid;

            [DefaultValue(0.5f)]
            [IncludeIf(nameof(IsOperandConstant))]
            public float Value;

            [IncludeIf(nameof(IsOperandGrid))]
            [ValidIf(nameof(ValidateGridSizesMatch))]
            public HeightGrid Grid2;
        }

        private bool IsOperandConstant() => Options.UseConstantOperand;
        private bool IsOperandGrid() => !Options.UseConstantOperand;

        private ValidationResult ValidateGridSizesMatch(InputValues inputs)
        {
            var classModel = ClassModelCache.GetClassModel<InputValues>();
            var grid1FieldModel = classModel.GetFieldModel(nameof(InputValues.Grid));
            var grid2FieldModel = classModel.GetFieldModel(nameof(InputValues.Grid2));

            return ValidationHelpers.ValidateGridSizesMatch(
                inputs.Grid, inputs.Grid2, grid1FieldModel.DisplayName, grid2FieldModel.DisplayName);
        }

        protected override void OnDefineCustomInputPorts(IPortDefinitionContext context)
        {
            var classModel = ClassModelCache.GetClassModel<InputValues>();

            if (IsOperandGrid())
            {
                var grid1Model = classModel.GetFieldModel(nameof(InputValues.Grid));
                grid1Model.DisplayName = "Grid 1";

                var grid2Model = classModel.GetFieldModel(nameof(InputValues.Grid2));
                grid2Model.DisplayName = "Grid 2";
            }
            else
            {
                var gridModel = classModel.GetFieldModel(nameof(InputValues.Grid));
                gridModel.DisplayName = "Grid";
            }

            // Build the ports automatically
            base.OnDefineCustomInputPorts(context);
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var isZeroIgnored = false;
                var isFlipped = false;
                var inputGrid = Inputs.Grid;
                var inputGrid2 = Inputs.Grid2;
                var value = Inputs.Value;

                var size = inputGrid.Size;

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (IsOperandConstant())
                {
                    var arithmeticOperator = ArithmeticNode.ArithmeticOperator.Multiply;

                    var inputTexture = inputGrid.RenderTexture;

                    if (!ShaderWrappers.TryArithmeticOperation(
                        inputTexture, value, arithmeticOperator, isZeroIgnored, isFlipped, size, ref outputTexture))
                    {
                        return false;
                    }
                }
                else
                {
                    var blendOperator = BlendNode.BlendOperator.Multiply;

                    var inputTexture1 = inputGrid.RenderTexture;
                    var inputTexture2 = inputGrid2.RenderTexture;

                    if (!ShaderWrappers.TryBlendOperation(
                        inputTexture1, inputTexture2, blendOperator, isZeroIgnored, isFlipped, size, ref outputTexture))
                    {
                        return false;
                    }
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