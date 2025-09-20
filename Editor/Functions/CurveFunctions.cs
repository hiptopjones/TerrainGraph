using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    public static class CurveFunctions
    {
        public enum CurveType
        {
            Line = 100,
            SmoothStep = 200,
            Parabolic,

            // Easings
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

        private static Dictionary<CurveType, Func<float, Vector2>> _functionLookup = new();

        public static bool TryGetFunction(CurveType curveType, out Func<float, Vector2> function)
        {
            function = null;

            if (_functionLookup.TryGetValue(curveType, out function))
            {
                return true;
            }

            // Include the easing functions as curves
            var definingTypes = new[] { typeof(CurveFunctions), typeof(EasingFunctions) };
            foreach (var definingType in definingTypes)
            {
                var methodName = curveType.ToString();
                var method = definingType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    continue;
                }

                function = (t) => new Vector2(t, (float)method.Invoke(null, new object[] { t }));
                _functionLookup[curveType] = function;

                return true;
            }

            Debug.LogError($"Unsupported curve type: {curveType}");
            return false;
        }

        public static float Line(float t)
        {
            return t;
        }

        public static float SmoothStep(float t)
        {
            return Mathf.SmoothStep(0, 1, t);
        }

        public static float Parabolic(float t)
        {
            var x = 2 * (t - 0.5f);
            return x * x;
        }
    }
}
