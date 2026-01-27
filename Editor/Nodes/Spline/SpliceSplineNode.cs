using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class SpliceSplineNode
        : BaseNode<SpliceSplineNode.OptionValues, SpliceSplineNode.InputValues, SplineWrapper>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            [DisplayName("Spline 1")]
            [Passthru]
            public SplineWrapper SplineWrapper1;

            [DisplayName("Spline 2")]
            public SplineWrapper SplineWrapper2;

            [MinValue(0)]
            public float Start;

            [MinValue(0), DefaultValue(0.5f)]
            [ValidIf(nameof(IsValidEnd))]
            public float End;

            [DisplayName("Vertices")]
            [MinValue(10), DefaultValue(100)]
            public int VertexCount;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    SplineWrapper1?.VersionHash, SplineWrapper2?.VersionHash, Start, End, VertexCount
                );
            }
        }

        private ValidationResult IsValidEnd(InputValues inputs)
        {
            var classModel = ClassModelCache.GetClassModel<InputValues>();
            var endModel = classModel.GetFieldModel(nameof(InputValues.End));

            if (inputs.Start >= inputs.End)
            {
                inputs.End = inputs.Start + 0.001f;
                ValidationResult.Warning($"{endModel.DisplayName} input invalid: {inputs.End} (valid: start > end)");
            }

            return ValidationResult.Ok();
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var inputSplineWrapper1 = Inputs.SplineWrapper1;
                var inputSplineWrapper2 = Inputs.SplineWrapper2;
                var vertexCount = Inputs.VertexCount;
                var start = Inputs.Start;
                var end = Inputs.End;

                var inputSpline1 = inputSplineWrapper1.Spline;
                var inputSpline2 = inputSplineWrapper2.Spline;

                // Prepare the partial spline for "seamless" splicing
                var transformedSpline2 = TransformSpline(inputSpline1, inputSpline2, start, end);

                var points = new List<float3>();

                var interval = 1 / (float)(vertexCount - 1);

                for (var t = 0f; t < start; t += interval)
                {
                    var position = SplineUtility.EvaluatePosition(inputSpline1, t);
                    points.Add(position);
                }

                for (var t1 = start; t1 < end; t1 += interval)
                {
                    var t2 = Mathf.InverseLerp(start, end, t1);
                    var position = SplineUtility.EvaluatePosition(transformedSpline2, t2);
                    points.Add(position);
                }

                for (var t = end; t < 1f; t += interval)
                {
                    var position = SplineUtility.EvaluatePosition(inputSpline1, t);
                    points.Add(position);
                }

                if (!inputSpline1.Closed)
                {
                    var lastPosition = SplineUtility.EvaluatePosition(inputSpline1, 1);
                    points.Add(lastPosition);
                }

                var outputSpline = new Spline(points);
                outputSpline.Closed = inputSpline1.Closed;

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

        private Spline TransformSpline(Spline fullSpline, Spline partialSpline, float t1, float t2)
        {
            Vector3 fp1 = fullSpline.EvaluatePosition(t1);
            Vector3 fp2 = fullSpline.EvaluatePosition(t2);

            Vector3 pp1 = partialSpline.EvaluatePosition(0f);
            Vector3 pp2 = partialSpline.EvaluatePosition(1f);

            Vector3 fullDirection = (fp2 - fp1);
            float fullLength = fullDirection.magnitude;

            Vector3 partialDirection = (pp2 - pp1);
            float partialLength = partialDirection.magnitude;

            float scale = fullLength / partialLength;
            Quaternion rotation = Quaternion.FromToRotation(partialDirection, fullDirection);
            Vector3 offset = fp1 - rotation * (pp1 * scale);

            var transformedPoints = new List<Vector2>();
            foreach (var knot in partialSpline.Knots)
            {
                Vector3 transformedPosition = rotation * (knot.Position * scale) + offset;
                transformedPoints.Add(new Vector2(transformedPosition.x, transformedPosition.z));
            }

            return SplineHelpers.CreateSpline(transformedPoints);
        }
    }
}