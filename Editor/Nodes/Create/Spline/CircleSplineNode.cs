using System;
using UnityEngine;
using UnityEngine.Splines;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class CircleSplineNode
        : BaseNode<CircleSplineNode.OptionValues, CircleSplineNode.InputValues, SplineWrapper>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            [MinValue(16), DefaultValue(256)]
            public int Size;

            [RangeValue(1, 360), DefaultValue(360)]
            public float AngleDegrees;

            [DisplayName("Vertices")]
            [MinValue(10), DefaultValue(10)]
            public int VertexCount;
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var angleDegrees = Inputs.AngleDegrees;
                var size = Inputs.Size;
                var vertexCount = Inputs.VertexCount;

                if (!TryGetSpline(angleDegrees, size, vertexCount, out var outputSpline))
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

        public bool TryGetSpline(float angleDegrees, int size, int vertexCount, out Spline spline)
        {
            var radius = size / 2f;
            var center = Vector2.one * radius;
            var interval = angleDegrees / vertexCount;

            spline = SplineFunctions.Circle(radius, angleDegrees, interval, center);
            return true;
        }
    }
}