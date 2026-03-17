using System;
using UnityEngine;

namespace CodeFirst.TerrainGraph.Editor
{
    [Serializable]
    public class TransformNode
        : BaseNode<TransformNode.OptionValues, TransformNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            [Passthru]
            public HeightGrid Grid;

            public Vector2 TranslationPercent;

            public float RotationDegrees;

            public Vector2 Scale;
        }

        protected override void OnDefineCustomInputPorts(IPortDefinitionContext context)
        {
            var classModel = ClassModelCache.GetClassModel<InputValues>();

            var scaleModel = classModel.GetFieldModel(nameof(InputValues.Scale));
            scaleModel.DefaultValue = Vector2.one;

            // Build the ports automatically
            base.OnDefineCustomInputPorts(context);
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var inputGrid = Inputs.Grid;
                var translationPercent = Inputs.TranslationPercent;
                var rotationDegrees = Inputs.RotationDegrees;
                var scale = Inputs.Scale;

                var size = inputGrid.Size;

                var inputTexture = inputGrid.RenderTexture;
                var outputTexture = GetOrCreateNodeRenderTexture(size);

                var translation = translationPercent * size;

                if (!ShaderWrappers.TryTransformOperation(
                    inputTexture, translation, rotationDegrees, scale, size, ref outputTexture))
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