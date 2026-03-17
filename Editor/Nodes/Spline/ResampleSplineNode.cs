using System;
using UnityEngine;

namespace CodeFirst.TerrainGraph.Editor
{
    [Serializable]
    public class ResampleSplineNode
        : BaseNode<ResampleSplineNode.OptionValues, ResampleSplineNode.InputValues, SplineWrapper>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            [DisplayName("Spline")]
            [Passthru]
            public SplineWrapper SplineWrapper;

            [DisplayName("Vertices")]
            [MinValue(10), DefaultValue(100)]
            public int VertexCount;
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var inputSplineWrapper = Inputs.SplineWrapper;
                var vertexCount = Inputs.VertexCount;

                var inputSpline = inputSplineWrapper.Spline;

                var outputSpline = SplineHelpers.ResampleSpline(inputSpline, vertexCount);

                var outputSplineWrapper = new SplineWrapper
                {
                    Spline = outputSpline
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