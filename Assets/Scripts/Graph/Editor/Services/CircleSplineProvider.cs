using UnityEngine;
using UnityEngine.Splines;

public class CircleSplineProvider : SplineProvider
{
    public int Size { get; set; }
    public float Angle { get; set; }

    public override bool IsValid => true;
    public override int VersionHash { get; set; }

    public override bool TryGetSpline(int vertexCount, out Spline spline)
    {
        var radius = Size / 2f;
        var center = Vector2.one * radius;
        var interval = Angle / vertexCount;
        
        spline = SplineFunctions.Circle(radius, Angle, interval,    center);
        return true;
    }
}
