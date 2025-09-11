using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

internal static class SplineHelpers
{
    // Applies any transform on the spline container, so the points are expressed in world space
    public static Spline GetWorldSpaceSpline(SplineContainer splineContainer)
    {
        if (splineContainer.Splines.Count == 0)
        {
            return null;
        }

        Spline sourceSpline = splineContainer.Spline;
        Spline worldSpline = new Spline(sourceSpline.Count, sourceSpline.Closed);

        Transform splineContainerTransform = splineContainer.transform;

        foreach (BezierKnot knot in sourceSpline)
        {
            // Transform the knot's position into world space
            Vector3 position = splineContainerTransform.TransformPoint(knot.Position);

            // Transform tangent directions
            Vector3 tangentIn = splineContainerTransform.TransformPoint(knot.Position + knot.TangentIn) - splineContainerTransform.TransformPoint(knot.Position);
            Vector3 tangentOut = splineContainerTransform.TransformPoint(knot.Position + knot.TangentOut) - splineContainerTransform.TransformPoint(knot.Position);

            BezierKnot newKnot = new BezierKnot(position, tangentIn, tangentOut, knot.Rotation);
            worldSpline.Add(newKnot);
        }

        return worldSpline;
    }

    public static Vector3[] GetSplineVertices(Spline spline, float distanceStep)
    {
        List<Vector3> pathVertices = new List<Vector3>();

        float t = 0;
        while (t < 1)
        {
            pathVertices.Add(spline.EvaluatePosition(t));

            // Find the next time based on a step distance
            SplineUtility.GetPointAtLinearDistance(spline, t, distanceStep, out t);
        }

        // Add the last position, as we didn't evaluate it above
        pathVertices.Add(spline.EvaluatePosition(1));

        return pathVertices.ToArray();
    }
     
    public static Spline CreateSpline(List<Vector2> points, bool closed = false)
    {
        return new Spline(points.Select(p => new float3(p.x, 0, p.y)), TangentMode.AutoSmooth, closed);
    }
}
