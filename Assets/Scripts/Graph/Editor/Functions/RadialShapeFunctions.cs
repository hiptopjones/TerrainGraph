using System;
using System.Reflection;
using UnityEngine;

public static class RadialShapeFunctions
{
    public enum ShapeType
    {
        Cone = 100,
        Cylinder = 200,
        Gaussian = 300,
        SmoothStep = 400,
    }

    public static bool TryGetFunction(ShapeType shapeType, out Func<Vector2, float, float> function)
    {
        function = null;

        var methodName = shapeType.ToString();
        var method = typeof(RadialShapeFunctions).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        if (method == null)
        {
            Debug.LogError($"Unsupported shape type: {shapeType}");
            return false;
        }

        function = (p, r) => (float)method.Invoke(null, new object[] { p, r });
        return true;
    }

    public static float Cone(Vector2 position, float radius)
    {
        var distance = position.magnitude;
        if (distance > radius)
        {
            return 0;
        }

        return 1 - distance / radius;
    }

    public static float Cylinder(Vector2 position, float radius)
    {
        var distance = position.magnitude;
        return distance > radius ? 0 : 1;
    }

    public static float Gaussian(Vector2 position, float radius)
    {
        // sigma chosen so that (3 * sigma) ~ distance to edge
        float sigma = radius / 3f;
        float twoSigmaSq = 2f * sigma * sigma;

        float dx = position.x;
        float dy = position.y;
        float distSq = dx * dx + dy * dy;

        float g = Mathf.Exp(-distSq / twoSigmaSq); // value in [0,1]
        return Mathf.Clamp01(g);
    }

    public static float SmoothStep(Vector2 position, float radius)
    {
        return RadialFunction((t) => Mathf.SmoothStep(0f, 1f, t), position, radius);
    }

    // Rotate any easing-like function around the center to get a (rounded) bump
    private static float RadialFunction(Func<float, float> function, Vector2 position, float radius)
    {
        // Distance from the center
        float distance = position.magnitude;

        // Normalize distance into [0,1] where 0 = center, 1 = edge of radius
        float t = Mathf.Clamp01(distance / radius);

        // Invert so that 0 = edge, 1 = center
        t = 1f - t;

        return function(t);
    }
}
