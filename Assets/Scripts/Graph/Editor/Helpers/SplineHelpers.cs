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
}
