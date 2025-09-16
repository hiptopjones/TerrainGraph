using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class SplineHeightProvider : IHeightProvider
{
    public Spline Spline { get; set; }
    public float Width { get; set; }
    public int Samples { get; set; }
    public bool Center { get; set; }

    public bool IsValid => true;
    public int VersionHash { get; set; }

    public bool TryGetHeights(int size, out float[,] heights)
    {
        var spline = Spline;

        if (Center)
        {
            var bounds = spline.GetBounds();

            var inputCenter = bounds.center;
            var outputCenter = new Vector3(size / 2f, 0, size / 2f);

            spline = new Spline(Spline);
            for (int i = 0; i < spline.Count; i++)
            {
                var knot = spline[i];
                knot.Position += (float3)(outputCenter - inputCenter);
                spline[i] = knot;
            }
        }

        return SplineSdfJobRunner.TryCreateSdf(spline, Samples, size, out heights, out _);
    }
}
