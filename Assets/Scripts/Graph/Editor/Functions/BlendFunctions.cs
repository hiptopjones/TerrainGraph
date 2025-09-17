using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class BlendFunctions
{
    public enum BlendMethod
    {
        Add = 100,
        Subtract = 200,
        Multiply = 300,
        Divide = 400,
        Minimum = 500,
        Maximum = 600,
        Average = 700,
        Compare = 1000,
    }

    private static Dictionary<BlendMethod, Func<float, float, float>> _functionLookup = new();

    public static bool TryGetFunction(BlendMethod blendMethod, out Func<float, float, float> function)
    {
        function = null;

        if (_functionLookup.TryGetValue(blendMethod, out function))
        {
            return true;
        }

        var methodName = blendMethod.ToString();
        var method = typeof(BlendFunctions).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        if (method == null)
        {
            Debug.LogError($"Unsupported blend method: {blendMethod}");
            return false;
        }

        function = (t1, t2) => (float)method.Invoke(null, new object[] { t1, t2 });
        _functionLookup[blendMethod] = function;

        return true;
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

    public static float Compare(float a, float b)
    {
        if (a == b)
        {
            return 0;
        }

        return a > b ? -1 : 1.1f; // over 1 so it's green in the preview
    }
}
