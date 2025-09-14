using System;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public static class CurveFunctions
{
    public enum CurveType
    {
        Line = 100,
        SmoothStep = 200,

        InQuad = 300,
        OutQuad,
        InOutQuad,

        InCubic,
        OutCubic,
        InOutCubic,

        InQuart,
        OutQuart,
        InOutQuart,

        InQuint,
        OutQuint,
        InOutQuint,

        InSine,
        OutSine,
        InOutSine,

        InExpo,
        OutExpo,
        InOutExpo,

        InCirc,
        OutCirc,
        InOutCirc,

        InElastic,
        OutElastic,
        InOutElastic,

        InBack,
        OutBack,
        InOutBack,

        InBounce,
        OutBounce,
        InOutBounce,
    }

    public static bool TryGetCurveFunction(CurveType curveType, out Func<float, Vector2> curveFunction)
    {
        curveFunction = null;

        switch (curveType)
        {
            case CurveType.Line:
                curveFunction = Line;
                return true;

            case CurveType.SmoothStep:
                curveFunction = SmoothStep;
                return true;

            default:
                if (TryGetEasingFunction(curveType, out curveFunction))
                {
                    return true;
                }

                Debug.LogError($"Unhandled curve type: {curveType}");
                return false;
        }
    }

    public static Vector2 Line(float t)
    {
        return new Vector2(t, t);
    }

    public static Vector2 SmoothStep(float t)
    {
        return new Vector2(t, Mathf.SmoothStep(0, 1, t));
    }

    private static bool TryGetEasingFunction(CurveType curveType, out Func<float, Vector2> easingFunction)
    {
        easingFunction = null;

        var methodName = curveType.ToString();
        var method = typeof(EasingFunctions).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        if (method == null)
        {
            return false;
        }

        easingFunction = (t) => new Vector2(t, (float)method.Invoke(null, new object[] { t }));
        return true;
    }
}
