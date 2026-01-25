using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class SmoothSplineNode
        : BaseNode<OptionValuesBase, SmoothSplineNode.InputValues, SplineWrapper>
    {
        public class InputValues : InputValuesBase
        {
            [DisplayName("Spline")]
            public SplineWrapper SplineWrapper;

            [DisplayName("Iterations")]
            [RangeValue(1, 100), DefaultValue(1)]
            public int IterationCount;

            [DisplayName("Min Angle")]
            [DefaultValue(150)]
            public float MinAngleDegrees;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    SplineWrapper?.VersionHash, IterationCount, MinAngleDegrees
                );
            }
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var inputSplineWrapper = Inputs.SplineWrapper;
                var iterationCount = Inputs.IterationCount;
                var minAngleDegrees = Inputs.MinAngleDegrees;

                var currentSpline = inputSplineWrapper.Spline;

                for (int i = 0; i < iterationCount; i++)
                {
                    var vertices = new List<Vector3>();

                    int startIndex = 0;
                    int endIndex = currentSpline.Count - 1;

                    if (!currentSpline.Closed)
                    {
                        startIndex++;
                        endIndex--;

                        vertices.Add(currentSpline.First().Position);
                    }

                    for (int j = startIndex; j <= endIndex; j++)
                    {
                        var j1 = (j - 1 + currentSpline.Count) % currentSpline.Count;
                        var j2 = j;
                        var j3 = (j + 1) % currentSpline.Count;

                        var p1 = currentSpline[j1].Position;
                        var p2 = currentSpline[j2].Position;
                        var p3 = currentSpline[j3].Position;

                        var angleDegrees = Vector3.Angle(p1 - p2, p3 - p2);
                        if (angleDegrees < minAngleDegrees)
                        {
                            var midpoint = (p1 + p3) / 2;
                            var t = Mathf.InverseLerp(minAngleDegrees, 0, angleDegrees);
                            p2 = Vector3.Lerp(p2, midpoint, t);
                        }

                        vertices.Add(p2);
                    }

                    if (!currentSpline.Closed)
                    {
                        vertices.Add(currentSpline.Last().Position);
                    }

                    var smoothedSpline = SplineHelpers.CreateSpline(vertices, currentSpline.Closed);
                    var resampledSpline = SplineHelpers.ResampleSpline(smoothedSpline, currentSpline.Count);

                    currentSpline = resampledSpline;
                }

                var outputSpline = currentSpline;

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