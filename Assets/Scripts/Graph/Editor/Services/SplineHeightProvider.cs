using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class SplineHeightProvider : IHeightProvider
{
    public Spline Spline { get; set; }
    public int Samples { get; set; }
    public bool Center { get; set; }
    public bool IncludeEdgeHeight { get; set; }

    public bool IsValid => true;
    public float ExecutionTime => 0;
    public int VersionHash { get; set; }

    public bool TryGetHeights(int size, out float[,] heights)
    {
        heights = null;

        var spline = Spline;

        if (Center)
        {
            var bounds = spline.GetBounds();

            var inputCenter = bounds.center.WithY(0);
            var outputCenter = new Vector3(size / 2f, 0, size / 2f);

            spline = new Spline(Spline);
            for (int i = 0; i < spline.Count; i++)
            {
                var knot = spline[i];
                knot.Position += (float3)(outputCenter - inputCenter);
                spline[i] = knot;
            }
        }

        if (!SplineSdfJobRunner.TryCreateSdf(spline, Samples, size, out var distances, out var nearestPositions))
        {
            return false;
        }

        if (IncludeEdgeHeight)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var distance = distances[x, y];

                    // Only apply spline heights for positive distances (inside the island)
                    if (distance <= 0)
                    {
                        continue;
                    }

                    float heightOffset = 0;

                    var splineY = nearestPositions[x, y].y;

                    var t = distance / 40f;

                    const float THRESHOLD = 0.3f;
                    if (t < 1)
                    {
                        if (t < THRESHOLD)
                        {
                            // Use the full range of the curve
                            t /= THRESHOLD;
                            heightOffset = splineY * EasingFunctions.OutCubic(t);
                        }
                        else
                        {
                            // Use the full range of the curve
                            t = (t - THRESHOLD) / (1 - THRESHOLD);
                            heightOffset = splineY * (1 - EasingFunctions.InOutCubic(t));
                        }

                        distances[x, y] += heightOffset;
                    }
                }
            }
        }

        heights = distances;
        return true;
    }
}
