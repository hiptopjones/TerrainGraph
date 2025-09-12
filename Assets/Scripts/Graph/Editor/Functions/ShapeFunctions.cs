using UnityEngine;

public static class ShapeFunctions
{
    public static float Invalid(Vector2 position, float radius)
    {
        return 1f;
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

}
