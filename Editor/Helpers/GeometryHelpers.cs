using System.Collections.Generic;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    internal class GeometryHelpers
    {
        // https://stackoverflow.com/questions/217578/how-can-i-determine-whether-a-2d-point-is-within-a-polygon
        public static bool IsPointInPolygon(Vector3 p, Vector3[] polygon, bool performSanityCheck = true)
        {
            if (!performSanityCheck)
            {
                float minX = polygon[0].x;
                float maxX = polygon[0].x;
                float minZ = polygon[0].z;
                float maxZ = polygon[0].z;
                for (int i = 1; i < polygon.Length; i++)
                {
                    Vector3 q = polygon[i];
                    minX = Mathf.Min(q.x, minX);
                    maxX = Mathf.Max(q.x, maxX);
                    minZ = Mathf.Min(q.z, minZ);
                    maxZ = Mathf.Max(q.z, maxZ);
                }

                if (p.x < minX || p.x > maxX || p.z < minZ || p.z > maxZ)
                {
                    return false;
                }
            }

            // https://wrf.ecse.rpi.edu/Research/Short_Notes/pnpoly.html
            bool isInside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if ((polygon[i].z > p.z) != (polygon[j].z > p.z) &&
                     p.x < (polygon[j].x - polygon[i].x) * (p.z - polygon[i].z) / (polygon[j].z - polygon[i].z) + polygon[i].x)
                {
                    isInside = !isInside;
                }
            }

            return isInside;
        }

        // f(x1, y1) q11    x      f(x2, y1) q21 
        //                  |
        //                  v
        //       y  -----> (p)
        // f(x1, y2) q12           f(x2, y2) q22
        public static float BilinearInterpolate(float x, float y, float q11, float q21, float q22, float q12, float x1, float y1, float x2, float y2)
        {
            // Calculate the interpolation factors
            float r1 = ((x2 - x) / (x2 - x1)) * q11 + ((x - x1) / (x2 - x1)) * q21;
            float r2 = ((x2 - x) / (x2 - x1)) * q12 + ((x - x1) / (x2 - x1)) * q22;

            // Perform the final interpolation
            // (this can introduce some precision errors, which matter if close to a height boundary)
            return ((y2 - y) / (y2 - y1)) * r1 + ((y - y1) / (y2 - y1)) * r2;
        }


        public static List<Vector2> GetConvexHull(List<Vector2> points)
        {
            if (points == null || points.Count < 3)
            {
                return new List<Vector2>(points);
            }

            // Step 1: Find the lowest point (pivot)
            Vector2 pivot = points[0];
            foreach (var p in points)
            {
                if (p.y < pivot.y || (Mathf.Approximately(p.y, pivot.y) && p.x < pivot.x))
                    pivot = p;
            }

            // Step 2: Sort points by polar angle relative to pivot
            points.Sort((a, b) =>
            {
                if (a == pivot)
                {
                    return -1;
                }
                if (b == pivot)
                {
                    return 1;
                }

                float angleA = Mathf.Atan2(a.y - pivot.y, a.x - pivot.x);
                float angleB = Mathf.Atan2(b.y - pivot.y, b.x - pivot.x);

                if (Mathf.Approximately(angleA, angleB))
                {
                    // If same angle, keep closer one first
                    float distA = (a - pivot).sqrMagnitude;
                    float distB = (b - pivot).sqrMagnitude;
                    return distA.CompareTo(distB);
                }

                return angleA.CompareTo(angleB);
            });

            // Step 3: Build hull using stack
            Stack<Vector2> hull = new Stack<Vector2>();
            hull.Push(points[0]);
            hull.Push(points[1]);

            for (int i = 2; i < points.Count; i++)
            {
                Vector2 top = hull.Pop();
                Vector2 nextToTop = hull.Peek();
                hull.Push(top);

                while (hull.Count >= 2 && Cross(nextToTop, top, points[i]) <= 0)
                {
                    hull.Pop();
                    if (hull.Count < 2)
                    {
                        break;
                    }

                    top = hull.Pop();
                    nextToTop = hull.Peek();
                    hull.Push(top);
                }

                hull.Push(points[i]);
            }

            return new List<Vector2>(hull);
        }

        // Cross a -> b with a -> c
        public static float Cross(Vector2 a, Vector2 b, Vector2 c)
        {
            return Cross(b - a, c - a);
        }

        public static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        // Ramer–Douglas–Peucker
        public static List<Vector2> SimplifyPolyline(List<Vector2> points, float epsilon)
        {
            if (points == null || points.Count < 2)
            {
                return points;
            }

            var index = -1;
            var maxDistance = 0f;

            // Find point farthest from line between first & last
            for (int i = 1; i < points.Count - 1; i++)
            {
                var distance = PerpendicularDistance(points[i], points[0], points[^1]);
                if (distance > maxDistance)
                {
                    index = i;
                    maxDistance = distance;
                }
            }

            // If max distance > epsilon, recursively simplify
            if (maxDistance > epsilon)
            {
                var left = SimplifyPolyline(points.GetRange(0, index + 1), epsilon);
                var right = SimplifyPolyline(points.GetRange(index, points.Count - index), epsilon);

                // Merge results (remove duplicate middle point)
                var result = new List<Vector2>(left);
                result.RemoveAt(result.Count - 1);
                result.AddRange(right);

                return result;
            }
            else
            {
                // Just keep endpoints
                return new List<Vector2> { points[0], points[^1] };
            }
        }


        public static float PerpendicularDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            if (a == b)
            {
                return Vector2.Distance(p, a);
            }

            var ab = b - a;
            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab));
            var q = a + t * ab;
            var pq = p - q;

            var distance = pq.magnitude;
            return distance;
        }
    }
}
