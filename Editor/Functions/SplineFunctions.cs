using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace Indiecat.TerrainGraph.Editor
{
    public static class SplineFunctions
    {
        public static Spline Circle(float radius, float angle, float interval, Vector2 center)
        {
            var points = new List<Vector2>();

            for (float theta = 0; theta < angle; theta += interval)
            {
                // Go clockwise from the top
                var radians = (90 - theta) * Mathf.Deg2Rad;

                float x = center.x + radius * Mathf.Cos(radians);
                float y = center.y + radius * Mathf.Sin(radians);

                points.Add(new Vector2(x, y));
            }

            var spline = SplineHelpers.CreateSpline(points, angle == 360);
            return spline;
        }
    }
}
