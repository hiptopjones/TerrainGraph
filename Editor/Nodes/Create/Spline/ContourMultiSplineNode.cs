using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace CodeFirst.TerrainGraph.Editor
{
    [Serializable]
    [Node(categoryPath: "Create/Spline", iconPath: null, title: "Multi Contour Spline")]
    public class ContourMultiSplineNode
        : BaseNode<ContourMultiSplineNode.OptionValues, ContourMultiSplineNode.InputValues, SplineListWrapper>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            public HeightGrid Grid;

            [DisplayName("Height")]
            [DefaultValue(0.3f)]
            public float ContourHeight;

            [DisplayName("Relax")]
            public bool RelaxContour;

            [DisplayName("Vertices")]
            [MinValue(10), DefaultValue(100)]
            public int VertexCount;
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var inputGrid = Inputs.Grid;
                var contourHeight = Inputs.ContourHeight;
                var relaxContour = Inputs.RelaxContour;
                var vertexCount = Inputs.VertexCount;

                var size = inputGrid.Size;

                if (!ShaderWrappers.TryGenerateContours(inputGrid, contourHeight, relaxContour, vertexCount, size, out var outputSplines))
                {
                    return false;
                }

                var outputSplineListWrapper = new SplineListWrapper
                {
                    Splines = outputSplines,
                };

                outputSplineListWrapper.VersionHash = Inputs.VersionHash;

                CacheData.Output = outputSplineListWrapper;
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