using UnityEngine;
using UnityEngine.Splines;

public class CircleSplineProvider : SplineProvider
{
    public float Size { get; set; }

    public override bool IsValid => true;
    public override int VersionHash { get; set; }

    public override bool TryGetSpline(int vertexCount, out Spline spline)
    {
        var radius = Size / 2f;
        var center = Vector2.one * radius;
        var interval = 360f / vertexCount;
        
        spline = SplineFunctions.Circle(radius, interval, center);
        return true;
    }
}
