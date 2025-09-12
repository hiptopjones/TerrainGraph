using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public static class SplineFunctions
{
    public static Spline Circle(float radius, float interval, Vector2 center)
    {
        var points = new List<Vector2>();

        for (float angle = 0; angle < 360; angle += interval)
        {
            var radians = angle * Mathf.Deg2Rad;

            float x = center.x + radius * Mathf.Cos(radians);
            float y = center.y + radius * Mathf.Sin(radians);

            points.Add(new Vector2(x, y));
        }

        var spline = SplineHelpers.CreateSpline(points, closed: true);
        return spline;
    }
}
