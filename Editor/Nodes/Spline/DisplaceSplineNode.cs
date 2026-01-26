using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class DisplaceSplineNode
        : BaseNode<DisplaceSplineNode.OptionValues, DisplaceSplineNode.InputValues, SplineWrapper>
    {
        public enum DisplacementAxis
        {
            Horizontal,
            Vertical
        }

        public class OptionValues : OptionValuesBase
        {
            [DisplayName("Axis")]
            [DefaultValue(DisplacementAxis.Horizontal)]
            public DisplacementAxis DisplacementAxis;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    DisplacementAxis
                );
            }
        }

        public class InputValues : InputValuesBase
        {
            [DisplayName("Spline")]
            [Passthru]
            public SplineWrapper SplineWrapper;

            [DisplayName("Offset")]
            public float LinearOffset;

            [DefaultValue(2)]
            public float Frequency;

            [DefaultValue(30)]
            public float Amplitude;

            public int Seed;

            [DisplayName("Iterations")]
            [RangeValue(1, 10), DefaultValue(1)]
            public int IterationCount;

            [DisplayName("Vertices")]
            [MinValue(10), DefaultValue(100)]
            public int VertexCount;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    HashCode.Combine(SplineWrapper?.VersionHash, LinearOffset, Frequency),
                    Amplitude, Seed, IterationCount, VertexCount
                );
            }
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var displacementAxis = Options.DisplacementAxis;
                var inputSplineWrapper = Inputs.SplineWrapper;
                var offset = Inputs.LinearOffset;
                var frequency = Inputs.Frequency;
                var amplitude = Inputs.Amplitude;
                var seed = Inputs.Seed;
                var iterationCount = Inputs.IterationCount;
                var vertexCount = Inputs.VertexCount;

                var start = NoiseHelpers.GetOffsetPositionInternal(Vector2.zero, seed);

                var currentSpline = inputSplineWrapper.Spline;

                for (int i = 0; i < iterationCount; i++)
                {
                    var vertices = new List<Vector3>();

                    // TODO: Consider controlling whether the first and last vertex are displaced
                    for (int j = 0; j < vertexCount; j++)
                    {
                        var t = j / (float)(vertexCount - 1);
                        if (currentSpline.Closed)
                        {
                            // DO NOT add a vertex at t = 1 if it's closed
                            t = j / (float)vertexCount;
                        }

                        var position = currentSpline.EvaluatePosition(t);
                        var tangent = ((Vector3)currentSpline.EvaluateTangent(t)).normalized;

                        var up = Vector3.up;
                        var binormal = Vector3.Cross(up, tangent).normalized;

                        var displacement = Vector3.zero;

                        var noise = GetSeamlessNoise(start, frequency, t + offset);

                        switch (displacementAxis)
                        {
                            case DisplacementAxis.Horizontal:
                                displacement += binormal * noise * amplitude;
                                break;

                            case DisplacementAxis.Vertical:
                                displacement += up * Mathf.Clamp01(noise) * amplitude;
                                break;
                        }

                        var displacedPosition = (Vector3)position + displacement;
                        vertices.Add(displacedPosition);
                    }

                    var displacedSpline = SplineHelpers.CreateSpline(vertices, currentSpline.Closed);

                    currentSpline = displacedSpline;
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

        // Sample noise in a circle through the noise field, which makes it seamless
        public static float GetSeamlessNoise(Vector2 start, float frequency, float t)
        {
            var x = frequency * Mathf.Cos(t * 2 * Mathf.PI);
            var y = frequency * Mathf.Sin(t * 2 * Mathf.PI);

            var noise = NoiseHelpers.PerlinNoise2D(start.x + x, start.y + y);
            return noise;
        }
    }
}