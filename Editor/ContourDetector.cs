using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CodeFirst.TerrainGraph.Editor
{
    public static class ContourDetector
    {
        public static List<List<Vector2>> GetContours(List<KeyValuePair<Vector2, Vector2>> floatCellSegments, float value)
        {
            // The contours are currently a set of tiny unordered line segments from each grid cell.
            // It would draw fine, but not be useful for much else. So we go through a few steps
            // to collate, group and orient them first into larger connected segments and then
            // into complete contours.

            // Convert to int vectors to avoid precision errors
            var cellSegments = ConvertCellSegments(floatCellSegments);

            // Store connecteness for each point
            var adjacencyMap = CreateAdjacencyMap(cellSegments);

            // Turn the adjacency map into (incomplete) ordered lists of points
            var contourSegments = GetContourSegments(adjacencyMap);

            // Joins related segments together to complete the contours
            var contours = GetContours(contourSegments);

            // Ensures the returned contour has clockwise winding and has a deterministic start point
            var orientedContours = OrientContours(contours);

            // Convert back to float vectors
            var floatContours = ConvertContours(orientedContours);

            return floatContours;
        }

        private static List<List<Vector2Int>> OrientContours(List<List<Vector2Int>> contours)
        {
            var orientedContours = new List<List<Vector2Int>>();

            foreach (var contour in contours.OrderByDescending(c => c.Count))
            {
                var orientedContour = new List<Vector2Int>();

                // Find the minimum vertex by X and then by Y
                var minIndex = 0;
                var minVertex = Vector2Int.one * int.MaxValue;

                for (int i = 0; i < contour.Count; i++)
                {
                    var vertex = contour[i];

                    if (vertex.x < minVertex.x)
                    {
                        minVertex = vertex;
                        minIndex = i;
                    }
                    else if (vertex.x == minVertex.x)
                    {
                        if (vertex.y < minVertex.y)
                        {
                            minVertex = vertex;
                            minIndex = i;
                        }
                    }
                }

                // Enforce clockwise winding
                // Minimum vertex should be on the left side of the "clock"
                var p1 = contour[(minIndex - 1 + contour.Count) % contour.Count];
                var p2 = contour[(minIndex + 1) % contour.Count];
                if (p1.y > p2.y)
                {
                    contour.Reverse();
                    minIndex = (contour.Count - 1) - minIndex;
                }

                // Rotate the list, so the designated vertex is first (assumes the contour is closed)
                for (int i = minIndex; i < minIndex + contour.Count; i++)
                {
                    orientedContour.Add(contour[i % contour.Count]);
                }

                orientedContours.Add(orientedContour);
            }

            return orientedContours;
        }

        private static List<List<Vector2>> ConvertContours(List<List<Vector2Int>> contours)
        {
            var floatContours = new List<List<Vector2>>(contours.Count);

            foreach (var contour in contours)
            {
                var floatContour = new List<Vector2>(contour.Count);

                foreach (var point in contour)
                {
                    floatContour.Add(new Vector2(point.x / 1000f, point.y / 1000f));
                }

                floatContours.Add(floatContour);
            }

            return floatContours;
        }

        private static List<KeyValuePair<Vector2Int, Vector2Int>> ConvertCellSegments(List<KeyValuePair<Vector2, Vector2>> cellSegments)
        {
            var fixedSegments = new List<KeyValuePair<Vector2Int, Vector2Int>>(cellSegments.Count);

            foreach (var pair in cellSegments)
            {
                var p1 = pair.Key;
                var p2 = pair.Value;

                fixedSegments.Add(
                    new KeyValuePair<Vector2Int, Vector2Int>(
                        new Vector2Int(Mathf.RoundToInt(p1.x * 1000), Mathf.RoundToInt(p1.y * 1000)),
                        new Vector2Int(Mathf.RoundToInt(p2.x * 1000), Mathf.RoundToInt(p2.y * 1000))
                    )
                );
            }

            return fixedSegments;
        }

        private static Dictionary<Vector2Int, List<Vector2Int>> CreateAdjacencyMap(List<KeyValuePair<Vector2Int, Vector2Int>> segments)
        {
            var adjacencyMap = new Dictionary<Vector2Int, List<Vector2Int>>();

            foreach (var segment in segments)
            {
                var p1 = segment.Key;
                var p2 = segment.Value;

                if (!adjacencyMap.TryGetValue(p1, out var connections1))
                {
                    connections1 = new List<Vector2Int>();
                    adjacencyMap[p1] = connections1;
                }

                if (!adjacencyMap.TryGetValue(p2, out var connections2))
                {
                    connections2 = new List<Vector2Int>();
                    adjacencyMap[p2] = connections2;
                }

                connections1.Add(p2);
                connections2.Add(p1);
            }

            return adjacencyMap;
        }

        private static List<List<Vector2Int>> GetContourSegments(Dictionary<Vector2Int, List<Vector2Int>> adjacencyMap)
        {
            var visited = new HashSet<Vector2Int>();
            var contourSegments = new List<List<Vector2Int>>();

            while (adjacencyMap.Count > 0)
            {
                var contourSegment = GetSingleContourSegment(adjacencyMap, visited);
                contourSegments.Add(contourSegment);
            }

            return contourSegments;
        }

        private static List<Vector2Int> GetSingleContourSegment(Dictionary<Vector2Int, List<Vector2Int>> adjacencyMap, HashSet<Vector2Int> visited)
        {
            var contourSegment = new List<Vector2Int>();

            var first = adjacencyMap.First().Key;
            var p1 = first;

            while (true)
            {
                contourSegment.Add(p1);
                visited.Add(p1);

                if (!adjacencyMap.ContainsKey(p1))
                {
                    // This is an endpoint
                    break;
                }

                var p2 = PopConnectedPoint(adjacencyMap, p1);

                // Would this take us back the way we came?
                if (visited.Contains(p2))
                {
                    if (!adjacencyMap.ContainsKey(p1))
                    {
                        // This is an endpoint
                        break;
                    }

                    p2 = PopConnectedPoint(adjacencyMap, p1);
                }

                p1 = p2;
            }

            return contourSegment;
        }

        private static Vector2Int PopConnectedPoint(Dictionary<Vector2Int, List<Vector2Int>> adjacencyMap, Vector2Int p1)
        {
            var connections1 = adjacencyMap[p1];
            var p2 = connections1.First();

            var connections2 = adjacencyMap[p2];

            // Remove relevant connections
            connections1.Remove(p2);
            connections2.Remove(p1);

            if (connections1.Count == 0) adjacencyMap.Remove(p1);
            if (connections2.Count == 0) adjacencyMap.Remove(p2);

            return p2;
        }

        private static List<List<Vector2Int>> GetContours(List<List<Vector2Int>> contourSegments)
        {
            var contours = new List<List<Vector2Int>>();

            while (contourSegments.Count > 0)
            {
                var segment1 = contourSegments.First();
                contourSegments.Remove(segment1);

                var segmentIndex = 0;
                while (segmentIndex < contourSegments.Count)
                {
                    var segment2 = contourSegments[segmentIndex];

                    var start1 = segment1.First();
                    var end1 = segment1.Last();
                    var start2 = segment2.First();
                    var end2 = segment2.Last();

                    if (segment1.First() == segment2.First() ||
                        segment1.Last() == segment2.Last())
                    {
                        // Flip either one to make it right
                        segment2.Reverse();

                        segment1 = segment2.Concat(segment1.Skip(1)).ToList();
                        contourSegments.Remove(segment2);
                    }
                    else if (segment1.First() == segment2.Last())
                    {
                        segment1 = segment2.Concat(segment1.Skip(1)).ToList();
                        contourSegments.Remove(segment2);
                    }
                    else if (segment1.Last() == segment2.First())
                    {
                        segment1 = segment1.Concat(segment2.Skip(1)).ToList();
                        contourSegments.Remove(segment2);
                    }
                    else
                    {
                        segmentIndex++;
                    }
                }

                contours.Add(segment1);
            }

            return contours;
        }
    }
}
