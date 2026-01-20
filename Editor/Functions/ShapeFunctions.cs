using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace Indiecat.TerrainGraph.Editor
{
    public static class ShapeFunctions
    {
        public static Spline Square(int size)
        {
            var vertices = new List<Vector2>();

            var left = 0;
            var right = size - 1;
            var top = 0;
            var bottom = size - 1;

            vertices.Add(new Vector2(left, top));
            vertices.Add(new Vector2(right, top));
            vertices.Add(new Vector2(right, bottom));
            vertices.Add(new Vector2(left, bottom));

            var spline = SplineHelpers.CreateSpline(vertices, closed: true);
            return spline;
        }

        public static Spline Rectangle(int size)
        {
            var vertices = new List<Vector2>();

            var girth = size / 2;
            var left = 0;
            var right = size - 1;
            var top = girth - girth / 2;
            var bottom = top + girth;

            vertices.Add(new Vector2(left, top));
            vertices.Add(new Vector2(right, top));
            vertices.Add(new Vector2(right, bottom));
            vertices.Add(new Vector2(left, bottom));

            var spline = SplineHelpers.CreateSpline(vertices, closed: true);
            return spline;
        }

        public static Spline ThinRectangle(int size)
        {
            var vertices = new List<Vector2>();

            var girth = size / 4;
            var left = 0;
            var right = size - 1;
            var top = girth - girth / 2;
            var bottom = top + girth;

            vertices.Add(new Vector2(left, top));
            vertices.Add(new Vector2(right, top));
            vertices.Add(new Vector2(right, bottom));
            vertices.Add(new Vector2(left, bottom));

            var spline = SplineHelpers.CreateSpline(vertices, closed: true);
            return spline;
        }

        public static Spline RightAngle(int size)
        {
            var vertices = new List<Vector2>();

            var left = 0;
            var right = size - 1;
            var top = 0;
            var bottom = size - 1;

            var girth = size / 4;
            var rightInside = girth;
            var topInside = bottom - girth;

            vertices.Add(new Vector2(left, top));
            vertices.Add(new Vector2(rightInside, top));
            vertices.Add(new Vector2(rightInside, topInside));
            vertices.Add(new Vector2(right, topInside));
            vertices.Add(new Vector2(right, bottom));
            vertices.Add(new Vector2(left, bottom));

            var spline = SplineHelpers.CreateSpline(vertices, closed: true);
            return spline;
        }

        public static Spline SemiCircle(int size)
        {
            var vertices = new List<Vector2>();

            var center = size / 2;
            var radius = size / 2;

            for (int a = 0; a <= 180; a += 30)
            {
                var x = radius * Mathf.Cos(a * Mathf.Deg2Rad) + center;
                var y = radius * Mathf.Sin(a * Mathf.Deg2Rad) + center;
                vertices.Add(new Vector2(x, y));
            }

            var spline = SplineHelpers.CreateSpline(vertices, closed: true);
            return spline;
        }

        public static Spline Banana(int size)
        {
            var vertices = new List<Vector2>();

            var center = size / 2;
            var radius = size / 2;

            for (int a = 0; a <= 180; a += 30)
            {
                var x = radius * Mathf.Cos(a * Mathf.Deg2Rad) + center;
                var y = radius * Mathf.Sin(a * Mathf.Deg2Rad) + center;
                vertices.Add(new Vector2(x, y));
            }

            radius = size / 4;

            for (int a = 180; a >= 0; a -= 30)
            {
                var x = radius * Mathf.Cos(a * Mathf.Deg2Rad) + center;
                var y = radius * Mathf.Sin(a * Mathf.Deg2Rad) + center;
                vertices.Add(new Vector2(x, y));
            }

            var spline = SplineHelpers.CreateSpline(vertices, closed: true);
            return spline;
        }
    }
}
