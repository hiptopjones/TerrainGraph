using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace CodeFirst.TerrainGraph.Editor
{
    [Serializable]
    [Node(categoryPath: "Modify/Height", iconPath: null, title: "Rotate")]
    public class RotateNode
        : BaseNode<RotateNode.OptionValues, RotateNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            [Passthru]
            public HeightGrid Grid;

            public float RotationDegrees;
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var inputGrid = Inputs.Grid;
                var rotationDegrees = Inputs.RotationDegrees;

                var size = inputGrid.Size;

                var inputTexture = inputGrid.RenderTexture;
                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ShaderWrappers.TryTransformOperation(
                    inputTexture, Vector2.zero, rotationDegrees, Vector2.one, size, ref outputTexture))
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