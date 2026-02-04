using Indiecat.UnityCommon.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Indiecat.TerrainGraph.Editor
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Segment
    {
        public Vector3 a;
        public float _p0; // padding
        public Vector3 b;
        public float _p1; // padding
        public float t0;
        public float t1;
    }

    public static class SplineHelpers
    {
        public static List<Vector3> GetSampledSplinePoints(Spline spline, int stepCount)
        {
            var points = new List<Vector3>();

            var stepSize = 1f / stepCount;

            for (int i = 0; i < stepCount - 1; i++)
            {
                var t = i * stepSize;

                var point = spline.EvaluatePosition(t);
                points.Add(point);
            }

            // Add the end position, as we didn't evaluate it above
            points.Add(spline.EvaluatePosition(1));

            return points;
        }

        public static Spline CreateSpline(List<Vector2> vertices, bool isClosed = false)
        {
            return CreateSpline(vertices.Select(p => p.ToVector3XZ()).ToList(), isClosed: isClosed);
        }

        public static Spline CreateSpline(List<Vector3> vertices, bool isClosed = false)
        {
            return CreateSpline(vertices.Select(p => (float3)p).ToList(), isClosed: isClosed);
        }

        public static Spline CreateSpline(List<float3> vertices, bool isClosed = false)
        {
            var spline = new Spline(vertices, closed: isClosed);

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

        public static Spline GetCenteredSpline(Spline spline, Vector3 targetCenter)
        {
            var bounds = spline.GetBounds();

            // Ensure no Y component in either of the centers
            var sourceCenter = bounds.center.WithY(0);
            targetCenter = targetCenter.WithY(0);

            var vertices = new List<Vector3>();

            foreach (var knot in spline)
            {
                var vertex = targetCenter + (Vector3)knot.Position - sourceCenter;
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

        public static Spline GetTranslatedSpline(Spline spline, Vector3 translation)
        {
            var vertices = new List<Vector3>();

            foreach (var knot in spline)
            {
                var position = (Vector3)knot.Position;
                var vertex = position + translation;

                vertices.Add(vertex);
            }

            var translatedSpline = CreateSpline(vertices, spline.Closed);
            return translatedSpline;
        }

        public static Spline GetScaledSpline(Spline spline, Vector3 center, Vector3 scale)
        {
            var vertices = new List<Vector3>();

            foreach (var knot in spline)
            {
                var position = (Vector3)knot.Position;
                var centeredPosition = position - center;
                var scaledCenteredPosition = centeredPosition.PiecewiseMultiply(scale);

                var vertex = scaledCenteredPosition + center;
                vertices.Add(vertex);
            }

            var scaledSpline = CreateSpline(vertices, spline.Closed);
            return scaledSpline;
        }

        public static Spline GetTransformedSpline(Spline spline, int gridSize, bool centerSplineInGrid, bool scaleSplineToFitGrid)
        {
            if (centerSplineInGrid)
            {
                var gridCenter = (Vector2.one * gridSize / 2).ToVector3XZ();
                var splineCenter = GetCenter(spline).ToVector3XZ();

                if (scaleSplineToFitGrid)
                {
                    var splineBounds = spline.GetBounds();
                    var splineSize2d = splineBounds.size.SwizzleXZ();

                    // Provide some breathing room around the spline
                    var margin = Mathf.Clamp(gridSize / 20f, 5, 20);

                    var maxSize = splineSize2d.MaxComponent() + margin * 2;
                    var scale = (Vector3.one * (gridSize / maxSize)).WithY(1);

                    var scaledSpline = GetScaledSpline(spline, splineCenter, scale);

                    return GetCenteredSpline(scaledSpline, gridCenter);
                }

                return GetCenteredSpline(spline, gridCenter);
            }

            return spline;
        }

        public static List<Spline> CreateSplines(List<List<Vector2>> contours, int vertexCount)
        {
            var splines = new List<Spline>();

            var orderedContours = contours.OrderByDescending(x => x.Count);
            foreach (var contour in orderedContours)
            {
                var simplifiedContour = GeometryHelpers.SimplifyPolyline(contour, 2);
                //Debug.Log($"contour: {contour.Count} simplified: {simplifiedContour.Count}");

                var contourSpline = CreateSpline(simplifiedContour, isClosed: true);

                var spline = ResampleSpline(contourSpline, vertexCount);

                splines.Add(spline);
            }

            return splines;
        }

        public static List<Segment> GetSplineSegments(Spline spline, int segmentCount)
        {
            var segments = new List<Segment>(segmentCount);

            if (segmentCount <= 0)
            {
                return segments;
            }

            var pointCount = segmentCount + 1;
            var points = GetSampledSplinePoints(spline, pointCount);

            var td = 1f / segmentCount;
            var t = 0f;

            for (int i = 0; i < segmentCount; i++)
            {
                segments.Add(new Segment
                {
                    a = points[i],
                    b = points[i + 1],
                    t0 = t,
                    t1 = t + td,
                });

                t += td;
            }

            return segments;
        }
    }
}
