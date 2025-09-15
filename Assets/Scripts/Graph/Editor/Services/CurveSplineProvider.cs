using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class CurveSplineProvider : ISplineProvider
{
    public CurveFunctions.CurveType CurveType{ get; set; }
    public float Size { get; set; }

    public bool IsValid => true;
    public int VersionHash { get; set; }

    public bool TryGetSpline(int vertexCount, out Spline spline)
    {
        spline = null;

        if (!CurveFunctions.TryGetCurveFunction(CurveType, out var curveFunction))
        {
            return false;
        }

        var vertices = new List<Vector2>();

        for (int i = 0; i < vertexCount; i++)
        {
            var t = i / (float)(vertexCount - 1);
            var vertex = curveFunction(t) * Size;
            vertices.Add(vertex);
        }

        spline = new Spline(vertices.Select(p => new float3(p.x, 0, p.y)));
        return true;
    }
}
