using UnityEngine;

public static class BlendFunctions
{
    public static float Invalid(float a, float b)
    {
        return 1f;
    }

    public static float Add(float a, float b)
    {
        return a + b;
    }

    public static float Subtract(float a, float b)
    {
        return a - b;
    }

    public static float Multiply(float a, float b)
    {
        return a * b;
    }

    public static float Divide(float a, float b)
    {
        return a / b;
    }

    public static float Minimum(float a, float b)
    {
        return Mathf.Min(a, b);
    }

    public static float Maximum(float a, float b)
    {
        return Mathf.Max(a, b);
    }

    public static float Average(float a, float b)
    {
        return (a + b) / 2;
    }
}
