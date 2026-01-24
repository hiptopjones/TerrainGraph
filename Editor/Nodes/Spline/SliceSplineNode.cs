using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class SliceSplineNode
        : ExecutableNode<OptionValuesBase, SliceSplineNode.InputValues, SplineWrapper>
    {
        public class InputValues : InputValuesBase
        {
            [DisplayName("Spline")]
            public SplineWrapper SplineWrapper;

            [MinValue(0), DefaultValue(0)]
            public float Start;

            [MinValue(0), DefaultValue(0.5f)]
            public float End;

            [DisplayName("Vertices")]
            [MinValue(10), DefaultValue(100)]
            public int VertexCount;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    SplineWrapper?.VersionHash, Start, End, VertexCount
                );
            }
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var inputSplineWrapper = Inputs.SplineWrapper;
                var vertexCount = Inputs.VertexCount;
                var start = Inputs.Start;
                var end = Inputs.End;

                var inputSpline = inputSplineWrapper.Spline;

                var points = new List<float3>();

                for (int i = 0; i < vertexCount; i++)
                {
                    float t = Mathf.Lerp(start, end, i / (float)(vertexCount - 1));

                    var position = SplineUtility.EvaluatePosition(inputSpline, t);
                    points.Add(position);
                }

                var outputSpline = new Spline(points);

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