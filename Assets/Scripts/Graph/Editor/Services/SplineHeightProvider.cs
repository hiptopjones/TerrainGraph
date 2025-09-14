using UnityEngine;
using UnityEngine.Splines;

public class SplineHeightProvider : HeightProvider
{
    public Spline Spline { get; set; }
    public float Width { get; set; }

    public override bool IsValid => true;
    public override int VersionHash { get; set; }

    public override bool TryGetHeights(int size, out float[,] heights)
    {
        heights = new float[size, size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var position = new Vector3(x, 0, y);

                var distance = SplineUtility.GetNearestPoint(Spline, position, out var nearest, out var t);

                heights[x, y] = distance;
            }
        }

        return true;
    }
}
