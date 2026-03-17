using System;
using UnityEngine;

namespace CodeFirst.TerrainGraph.Editor
{
    [Serializable]
    public class BlendNode
        : BaseNode<BlendNode.OptionValues, BlendNode.InputValues, HeightGrid>
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
            [DisplayName("Operation")]
            public BlendOperator BlendOperator;

            [DisplayName("Ignore Zero")]
            public bool IsZeroIgnored;

            [DisplayName("Flip Inputs")]
            public bool IsFlipped;
        }

        public class InputValues : InputValuesBase
        {
            [Passthru]
            public HeightGrid Grid1;

            [ValidIf(nameof(ValidateGridSizesMatch))]
            public HeightGrid Grid2;
        }

        protected override void OnDefineCustomInputPorts(IPortDefinitionContext context)
        {
            if (Options.IsFlipped)
            {
                BuildInputPort(context, x => x.Grid2);
                BuildInputPort(context, x => x.Grid1);
            }
            else
            {
                BuildInputPort(context, x => x.Grid1);
                BuildInputPort(context, x => x.Grid2);
            }
        }

        private ValidationResult ValidateGridSizesMatch(InputValues inputs)
        {
            var classModel = ClassModelCache.GetClassModel<InputValues>();
            var grid1FieldModel = classModel.GetFieldModel(nameof(InputValues.Grid1));
            var grid2FieldModel = classModel.GetFieldModel(nameof(InputValues.Grid2));

            return ValidationHelpers.ValidateGridSizesMatch(
                inputs.Grid1, inputs.Grid2, grid1FieldModel.DisplayName, grid2FieldModel.DisplayName);
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var blendOperator = Options.BlendOperator;
                var isFlipped = Options.IsFlipped;
                var isZeroIgnored = Options.IsZeroIgnored;
                var inputGrid1 = Inputs.Grid1;
                var inputGrid2 = Inputs.Grid2;

                var size = inputGrid1.Size;

                var inputTexture1 = inputGrid1.RenderTexture;
                var inputTexture2 = inputGrid2.RenderTexture;
                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ShaderWrappers.TryBlendOperation(
                    inputTexture1, inputTexture2, blendOperator, isZeroIgnored, isFlipped, size, ref outputTexture))
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