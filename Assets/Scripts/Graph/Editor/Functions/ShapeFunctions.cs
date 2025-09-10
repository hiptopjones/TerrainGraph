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
}
