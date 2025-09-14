using UnityEngine.Splines;

public class SplineHeightProvider : HeightProvider
{
    public Spline Spline { get; set; }
    public float Width { get; set; }
    public int Samples { get; set; }

    public override bool IsValid => true;
    public override int VersionHash { get; set; }

    public override bool TryGetHeights(int size, out float[,] heights)
    {
        return SplineSdfJobRunner.TryCreateSdf(Spline, Samples, size, out heights);
    }
}
