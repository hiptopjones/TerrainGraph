using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using Indiecat.UnityCommon.Runtime;

namespace Indiecat.TerrainGraph.Editor
{
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

        public static List<Vector2> GetSplineVertices2d(Spline spline, int stepCount)
        {
            return GetSplineVertices2d(spline, spline.GetLength() / stepCount);
        }

        public static List<Vector3> GetSplineVertices3d(Spline spline, int stepCount)
        {
            return GetSplineVertices3d(spline, spline.GetLength() / stepCount);
        }

        public static List<Vector2> GetSplineVertices2d(Spline spline, float distanceStep)
        {
            var vertices = GetSplineVertices3d(spline, distanceStep);
            return vertices.Select(p => new Vector2(p.x, p.z)).ToList();
        }

        public static List<Vector3> GetSplineVertices3d(Spline spline, float distanceStep)
        {
            var vertices = new List<Vector3>();

            var t = 0f;

            while (t < 1)
            {
                vertices.Add(spline.EvaluatePosition(t));

                // Find the next time based on a step distance
                SplineUtility.GetPointAtLinearDistance(spline, t, distanceStep, out t);
            }

            if (!spline.Closed)
            {
                // Add the last position, as we didn't evaluate it above
                vertices.Add(spline.EvaluatePosition(1));
            }

            return vertices;
        }

        public static Spline CreateSpline(List<Vector2> vertices, bool closed = false)
        {
            return CreateSpline(vertices.Select(p => new float3(p.x, 0, p.y)).ToList(), closed: closed);
        }

        public static Spline CreateSpline(List<Vector3> vertices, bool closed = false)
        {
            return CreateSpline(vertices.Select(p => (float3)p).ToList(), closed: closed);
        }

        public static Spline CreateSpline(List<float3> vertices, bool closed = false)
        {
            var spline = new Spline(vertices, closed: closed);

            // NOTE: A closed spline doesn't seem to always smooth correctly between the last and first
            spline.SetTangentMode(TangentMode.AutoSmooth);

            return spline;
        }

        public static Spline ResampleSpline(Spline spline, int vertexCount)
        {
            var vertices = new List<float3>();

            for (int i = 0; i < vertexCount; i++)
            {
                var t = i / (float)(vertexCount - 1);
                if (spline.Closed)
                {
                    // DO NOT add a vertex at t = 1 if it's closed
                    t = i / (float)vertexCount;
                }

                var vertex = spline.EvaluatePosition(t);

                vertices.Add(vertex);
            }

            var outputSpline = CreateSpline(vertices, spline.Closed);
            return outputSpline;
        }


        public static Vector2 GetMinimumCenter(Spline spline, int margin = 0)
        {
            var size = GetMinimumBoundingSquareSize(spline, margin);
            var center = new Vector2(size / 2f, size / 2f);

            return center;
        }

        public static Spline GetCenteredSpline(Spline spline, int center)
        {
            return GetCenteredSpline(spline, new Vector3(center, 0, center));
        }

        public static Spline GetCenteredSpline(Spline spline, Vector2 center)
        {
            return GetCenteredSpline(spline, new Vector3(center.x, 0, center.y));
        }

        public static Spline GetCenteredSpline(Spline spline, Vector3 targetCenter)
        {
            var bounds = spline.GetBounds();

            // Ensure no Y component in either of the centers
            var sourceCenter = bounds.center.WithY(0);
            targetCenter = targetCenter.WithY(0);

            var vertices = new List<Vector3>();

            foreach (var knot in spline)
            {
                var vertex = targetCenter + knot.Position - sourceCenter;
                vertices.Add(vertex);
            }

            return CreateSpline(vertices, spline.Closed);
        }

        public static Vector2 GetCenter(Spline spline)
        {
            var bounds = spline.GetBounds();
            var center = bounds.center.SwizzleXZ();

            return center;
        }

        public static Bounds GetMinimumBoundingSquare(Spline spline, int margin = 0)
        {
            var bounds = spline.GetBounds();
            bounds.Expand(margin);

            var size = Mathf.CeilToInt(Mathf.Max(bounds.size.x, bounds.size.z));
            bounds.size = new Vector3(size, bounds.size.y, size);

            return bounds;
        }

        public static Bounds GetMinimumBoundingSquare(List<Spline> splines, int margin = 0)
        {
            var bounds = splines[0].GetBounds();

            foreach (var spline in splines.Skip(1))
            {
                bounds.Encapsulate(spline.GetBounds());
            }

            bounds.Expand(margin);

            var size = Mathf.CeilToInt(Mathf.Max(bounds.size.x, bounds.size.z));
            bounds.size = new Vector3(size, bounds.size.y, size);

            return bounds;
        }

        public static int GetMinimumBoundingSquareSize(Spline spline, int margin = 0)
        {
            var bounds = GetMinimumBoundingSquare(spline, margin);
            return (int)bounds.size.x;
        }

        public static int GetMinimumBoundingSquareSize(List<Spline> splines, int margin = 0)
        {
            var bounds = GetMinimumBoundingSquare(splines, margin);
            return (int)bounds.size.x;
        }

        public static int GetOriginBoundingSquareSize(Spline spline, int margin = 0)
        {
            var bounds = spline.GetBounds();
            bounds.Encapsulate(Vector3.zero);

            var size = Mathf.CeilToInt(Mathf.Max(bounds.size.x, bounds.size.z));
            size += margin * 2;

            return size;
        }

        public static Spline TranslateSpline(Spline spline, Vector2 translation)
        {
            var vertices = new List<Vector2>();

            foreach (var knot in spline)
            {
                var position = (Vector3)knot.Position;
                var vertex = position.SwizzleXZ() + translation;

                vertices.Add(vertex);
            }

            var translatedSpline = CreateSpline(vertices, spline.Closed);
            return translatedSpline;
        }

        public static List<Spline> CreateSplines(List<List<Vector2>> contours, int vertexCount)
        {
            var splines = new List<Spline>();

            var orderedContours = contours.OrderByDescending(x => x.Count);
            foreach (var contour in orderedContours)
            {
                var simplifiedContour = GeometryHelpers.SimplifyPolyline(contour, 2);
                //Debug.Log($"contour: {contour.Count} simplified: {simplifiedContour.Count}");

                var contourSpline = CreateSpline(simplifiedContour, closed: true);

                var spline = ResampleSpline(contourSpline, vertexCount);

                splines.Add(spline);
            }

            return splines;
        }
    }
}
