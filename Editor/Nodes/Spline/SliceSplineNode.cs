using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Windows;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class SliceSplineNode
        : BaseNode<SliceSplineNode.OptionValues, SliceSplineNode.InputValues, SplineWrapper>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            [DisplayName("Spline")]
            [Passthru]
            public SplineWrapper SplineWrapper;

            [MinValue(0), DefaultValue(0)]
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
                    base.GetHashCode(),
                    SplineWrapper?.VersionHash, Start, End, VertexCount
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