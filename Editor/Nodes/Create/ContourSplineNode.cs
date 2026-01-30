using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ContourSplineNode
        : BaseNode<ContourSplineNode.OptionValues, ContourSplineNode.InputValues, SplineWrapper>
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

            [MinValue(0), DefaultValue(0)]
            public int ContourIndex;

            [DisplayName("Vertices")]
            [MinValue(10), DefaultValue(10)]
            public int VertexCount;
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var inputGrid = Inputs.Grid;
                var contourHeight = Inputs.ContourHeight;
                var contourIndex = Inputs.ContourIndex;
                var vertexCount = Inputs.VertexCount;

                var size = inputGrid.Size;

                if (!ShaderWrappers.TryGenerateContour(inputGrid, contourHeight, contourIndex, vertexCount, size, out var outputSpline))
                {
                    return false;
                }

                var outputSplineWrapper = new SplineWrapper
                {
                    Spline = outputSpline,
                };

                outputSplineWrapper.VersionHash = Inputs.VersionHash;

                CacheData.Output = outputSplineWrapper;
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