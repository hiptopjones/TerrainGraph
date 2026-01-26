using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class SplineCurvatureHeightNode
        : BaseNode<SplineCurvatureHeightNode.OptionValues, SplineCurvatureHeightNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            [DisplayName("Spline")]
            public SplineWrapper SplineWrapper;

            [MinValue(16), DefaultValue(256)]
            public int Size;

            [DisplayName("Samples")]
            [MinValue(10), DefaultValue(100)]
            public int SampleCount;

            [DisplayName("Threshold")]
            [DefaultValue(0.1f)]
            public float StraightThreshold;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    SplineWrapper?.VersionHash, Size, SampleCount, StraightThreshold
                );
            }
        }

        protected override bool TryExecuteNodeInternal()
        {
            Texture2D tempTexture = null;

            try
            {
                var inputSplineWrapper = Inputs.SplineWrapper;
                var size = Inputs.Size;
                var sampleCount = Inputs.SampleCount;
                var straightThreshold = Inputs.StraightThreshold;

                var inputSpline = inputSplineWrapper.Spline;
                var sampleDistance = inputSpline.GetLength() / sampleCount;

                if (!TryGetCurvatureSegments(inputSpline, sampleCount, straightThreshold, out var segments))
                {
                    return false;
                }

                tempTexture = TextureHelpers.CreateTexture(size, size, TextureFormat.RFloat);

                var pixels = new List<Color>(size * size);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        var position = new Vector2(x, y);

                        var height = 0f;

                        foreach (var segment in segments)
                        {
                            foreach (var segmentPosition in segment.Positions)
                            {
                                var radius = sampleDistance / 2;

                                var distance = (position - segmentPosition).magnitude;
                                if (distance <= radius)
                                {
                                    switch (segment.Curvature)
                                    {
                                        case CurvatureType.Concave:
                                            height = 100;
                                            break;
                                        case CurvatureType.Convex:
                                            height = -100;
                                            break;
                                        case CurvatureType.Straight:
                                            height = 1;
                                            break;
                                        case CurvatureType.Unknown:
                                            height = 0.5f;
                                            break;
                                    }

                                    break;
                                }
                            }
                        }

                        pixels.Add(new Color(height, 0, 0));
                    }
                }

                tempTexture.SetPixels(pixels.ToArray());
                tempTexture.Apply();

                var outputTexture = GetOrCreateNodeRenderTexture(size);
                Graphics.Blit(tempTexture, outputTexture);

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
            finally
            {
                if (tempTexture != null)
                {
                    Object.DestroyImmediate(tempTexture);
                    tempTexture = null;
                }
            }
        }

        private enum CurvatureType
        {
            Unknown,
            Straight,
            Convex,
            Concave
        }

        private class CurvatureSegment
        {
            public CurvatureType Curvature;
            public float ArcLength;
            public int StartIndex;
            public int Count;
            public List<Vector2> Positions;
            public float AverageCross;
        }

        private static bool TryGetCurvatureSegments(Spline inputSpline, int sampleCount, float straightThreshold, out List<CurvatureSegment> segments)
        {
            segments = new List<CurvatureSegment>();

            if (!TryGetCurvatures(inputSpline, sampleCount, straightThreshold, out var curvatures, out var crosses, out var positions))
            {
                Debug.Log("Failed to get curvatures");
                return false;
            }

            var distances = GetDistances(positions);

            var startIndex = 0;

            while (startIndex < sampleCount - 1)
            {
                var curvature = curvatures.Skip(startIndex).First();
                var count = curvatures.Skip(startIndex).TakeWhile(x => x == curvature).Count();

                var segment = new CurvatureSegment
                {
                    Curvature = curvature,
                    ArcLength = distances.Skip(startIndex).Take(count + 1).Sum(),
                    StartIndex = startIndex,
                    Count = count + 1,
                    Positions = positions.Skip(startIndex).Take(count + 1).ToList(),
                    AverageCross = crosses.Skip(startIndex).Take(count + 1).Average(),
                };

                segments.Add(segment);

                startIndex += count + 1;
            }

            return true;
        }

        private static List<float> GetDistances(List<Vector2> positions)
        {
            var distances = new List<float>();

            distances.Add(0);

            for (int i = 1; i < positions.Count; i++)
            {
                var p1 = positions[i - 1];
                var p2 = positions[i];

                var distance = (p2 - p1).magnitude;

                distances.Add(distance);
            }

            return distances;
        }

        private static bool TryGetCurvatures(Spline spline, int sampleCount, float straightThreshold, out List<CurvatureType> curvatures, out List<float> crosses, out List<Vector2> positions)
        {
            curvatures = new List<CurvatureType>();
            crosses = new List<float>();
            positions = new List<Vector2>();

            // Get the sample positions
            for (int i = 0; i < sampleCount; i++)
            {
                var t = i / (float)(sampleCount - 1);

                Vector3 position = spline.EvaluatePosition(t);
                positions.Add(new Vector2(position.x, position.z));
            }

            curvatures.Add(CurvatureType.Unknown);
            crosses.Add(0);

            // Cut off the ends
            for (int i = 1; i < sampleCount - 1; i++)
            {
                var p1 = positions[i - 1];
                var p2 = positions[i];
                var p3 = positions[i + 1];

                var cross = GeometryHelpers.Cross(p2 - p1, p3 - p2);
                crosses.Add(cross);

                var curvature = GetCurvature(cross, straightThreshold);
                curvatures.Add(curvature);
            }

            curvatures.Add(CurvatureType.Unknown);
            crosses.Add(0);

            return true;
        }

        private static CurvatureType GetCurvature(float cross, float straightThreshold)
        {
            if (Mathf.Abs(cross) < straightThreshold)
            {
                return CurvatureType.Straight;
            }

            return cross > 0 ? CurvatureType.Convex : CurvatureType.Concave;
        }
    }
}