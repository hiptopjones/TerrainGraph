using UnityEngine;
using UnityEngine.Splines;

public class CircleSplineProvider : SplineProvider
{
    public float Radius { get; set; }

    public override bool IsValid => true;
    public override int VersionHash { get; set; }

    public override bool TryGetSpline(int vertexCount, out Spline spline)
    {
        var center = Vector2.one * Radius;
        var interval = 360f / vertexCount;
        
        spline = SplineFunctions.Circle(Radius, interval, center);
        return true;
    }
}
