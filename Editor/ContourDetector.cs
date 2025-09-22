using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    public class ContourDetector
    {
        private float[,] _values;

        public Action<float[,], float> DetectionStart;
        public Action<int, int, float, byte> SampleResult;
        public Action<int, int, Vector2, Vector2> SegmentResult;
        public Action<int, int> IgnoredResult;

        public ContourDetector(HeightGrid grid)
        {
            _values = grid.GetHeights();
        }

        public ContourDetector(float[,] values)
        {
            _values = values;
        }

        public Dictionary<float, List<List<Vector2>>> DetectContours(float start, float step, int count)
        {
            var levels = Enumerable.Range(0, count)
                .Select(i => Mathf.Round((start + i * step) * 100) / 100) // Multiply/divide to avoid precision errors
                .ToArray();

            return DetectContours(levels);
        }

        public Dictionary<float, List<List<Vector2>>> DetectContours(float[] levels)
        {
            var contours = new Dictionary<float, List<List<Vector2>>>();

            foreach (float level in levels)
            {
                contours[level] = DetectContours(level);
            }

            return contours;
        }

        public List<List<Vector2>> DetectContours(float level)
        {
            DetectionStart?.Invoke(_values, level);

            var samples = GetSampleGrid(level);
            var segments = GetCellSegments(samples, level);
            var contours = GetContours(segments, level);

            return contours;
        }

        // Returns a grid that indicates if each cell is above or below the threshold level
        private byte[,] GetSampleGrid(float level)
        {
            var width = _values.GetLength(0);
            var height = _values.GetLength(1);

            byte[,] samples = new byte[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var value = _values[x, y];
                    if (level > value)
                    {
                        samples[x, y] = 1;
                    }

                    SampleResult?.Invoke(x, y, value, samples[x, y]);
                }
            }

            return samples;
        }

        private List<KeyValuePair<Vector2, Vector2>> GetCellSegments(byte[,] samples, float level)
        {
            var cellSegments = new List<KeyValuePair<Vector2, Vector2>>();

            var width = samples.GetLength(0);
            var height = samples.GetLength(1);

            // March the squares
            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    // Known grid positions
                    // a b
                    // c d
                    var ax = x;
                    var ay = y;
                    var bx = x + 1;
                    var by = y;
                    var cx = x;
                    var cy = y + 1;
                    var dx = x + 1;
                    var dy = y + 1;

                    // Values at grid positions
                    var va = _values[ax, ay];
                    var vb = _values[bx, by];
                    var vc = _values[cx, cy];
                    var vd = _values[dx, dy];

                    // Interpolated positions along grid lines
                    // https://jamie-wong.com/2014/08/19/metaballs-and-marching-squares/
                    // NOTE: The above suggests: bdy = by + (dy - by) * (1 - vb) / (vd - vb)
                    //  * (dy - by) is always 1 for our scenario, so it can be removed
                    //  * (1 - vb) is (level - vb) for us, because the 1 is the value for the contour
                    var abx = ax + (level - va) / (vb - va);
                    var cdx = cx + (level - vc) / (vd - vc);
                    var acy = ay + (level - va) / (vc - va);
                    var bdy = by + (level - vb) / (vd - vb);

                    // Endpoint positions
                    var px = 0f;
                    var py = 0f;
                    var qx = 0f;
                    var qy = 0f;

                    // Samples
                    var sa = samples[ax, ay];
                    var sb = samples[bx, by];
                    var sc = samples[cx, cy];
                    var sd = samples[dx, dy];

                    // Build key used for lookup table
                    int configuration =
                        (sa * 1) + (sb * 2) +
                        (sc * 8) + (sd * 4);

                    // Configuration mappings
                    // 0    1    2    3    4    5    6    7 
                    // - -  + -  - +  + +  - -  - +  - +  + +
                    // - -  - -  - -  - -  - +  + -  - +  - +
                    // 8    9    10   11   12   13   14   15  
                    // - -  + -  - +  + +  - -  + -  - +  + +
                    // + -  + -  + -  + -  + +  + +  + +  + +
                    switch (configuration)
                    {
                        // - - 
                        // - - 
                        case 0:
                            // Ignore full squares
                            break;

                        // + - 
                        // - - 
                        case 1:
                            px = ax;
                            py = acy;
                            qx = abx;
                            qy = ay;
                            break;

                        // - + 
                        // - - 
                        case 2:
                            px = abx;
                            py = ay;
                            qx = bx;
                            qy = bdy;
                            break;

                        // + + 
                        // - - 
                        case 3:
                            px = ax;
                            py = acy;
                            qx = bx;
                            qy = bdy;
                            break;

                        // 4   
                        // - - 
                        // - + 
                        case 4:
                            px = cdx;
                            py = cy;
                            qx = bx;
                            qy = bdy;
                            break;

                        // 5   
                        // - + 
                        // + - 
                        case 5:
                            // Ignore saddle points
                            break;

                        // 6   
                        // - + 
                        // - + 
                        case 6:
                            px = abx;
                            py = ay;
                            qx = cdx;
                            qy = cy;
                            break;

                        //  7 
                        //  + +
                        //  - +
                        case 7:
                            px = ax;
                            py = acy;
                            qx = cdx;
                            qy = cy;
                            break;

                        // 8   
                        // - - 
                        // + - 
                        case 8:
                            px = ax;
                            py = acy;
                            qx = cdx;
                            qy = cy;
                            break;

                        // 9   
                        // + - 
                        // + - 
                        case 9:
                            px = abx;
                            py = ay;
                            qx = cdx;
                            qy = cy;
                            break;

                        // 10  
                        // - + 
                        // + - 
                        case 10:
                            // Ignore saddle points
                            break;

                        // 11  
                        // + + 
                        // + - 
                        case 11:
                            px = cdx;
                            py = cy;
                            qx = bx;
                            qy = bdy;
                            break;

                        // 12  
                        // - - 
                        // + + 
                        case 12:
                            px = ax;
                            py = acy;
                            qx = bx;
                            qy = bdy;
                            break;

                        // 13  
                        // + - 
                        // + + 
                        case 13:
                            px = abx;
                            py = ay;
                            qx = bx;
                            qy = bdy;
                            break;

                        // 14  
                        // - + 
                        // + + 
                        case 14:
                            px = ax;
                            py = acy;
                            qx = abx;
                            qy = ay;

                            break;

                        // 15  
                        // + +
                        // + +
                        case 15:
                            // Ignore full squares
                            break;

                        default:
                            throw new Exception($"Unexpected configuration: {configuration}");
                    }

                    // If endpoints were calculated, add the segment
                    if (px != qx || py != qy)
                    {
                        var p1 = new Vector2(px, py);
                        var p2 = new Vector2(qx, qy);

                        var cellSegment = new KeyValuePair<Vector2, Vector2>(p1, p2);
                        cellSegments.Add(cellSegment);

                        SegmentResult?.Invoke(x, y, p1, p2);
                    }
                    else
                    {
                        IgnoredResult?.Invoke(x, y);
                    }
                }
            }

            return cellSegments;
        }

        private List<List<Vector2>> GetContours(List<KeyValuePair<Vector2, Vector2>> cellSegments, float value)
        {
            // The contours are currently a set of tiny unordered line segments from each grid cell.
            // It would draw fine, but not be useful for much else. So we go through a few steps
            // to collate, group and orient them first into larger connected segments and then
            // into complete contours.

            Debug.Log($"cell segments: {cellSegments.Count}");

            // Store connecteness for each point
            var adjacencyMap = CreateAdjacencyMap(cellSegments);

            Debug.Log($"adjaceny: {adjacencyMap.Count}");

            // Turn the adjacency map into (incomplete) ordered lists of points
            var contourSegments = GetContourSegments(adjacencyMap);

            Debug.Log($"contour segments: {contourSegments.Count}");

            // Joins related segments together to complete the contours
            var contours = GetContours(contourSegments);

            Debug.Log($"contours: {contours.Count}");

            // Ensures contours always transit a consistent direction
            var orientedContours = GetOrientedContours(contours, value);

            Debug.Log($"oriented contours: {orientedContours.Count}");

            return orientedContours;
        }

        private Dictionary<Vector2, List<Vector2>> CreateAdjacencyMap(List<KeyValuePair<Vector2, Vector2>> segments)
        {
            var adjacencyMap = new Dictionary<Vector2, List<Vector2>>();

            foreach (var segment in segments)
            {
                var p1 = segment.Key;
                var p2 = segment.Value;

                if (!adjacencyMap.TryGetValue(p1, out var connections1))
                {
                    connections1 = new List<Vector2>();
                    adjacencyMap[p1] = connections1;
                }

                if (!adjacencyMap.TryGetValue(p2, out var connections2))
                {
                    connections2 = new List<Vector2>();
                    adjacencyMap[p2] = connections2;
                }

                connections1.Add(p2);
                connections2.Add(p1);
            }

            return adjacencyMap;
        }

        private List<List<Vector2>> GetContourSegments(Dictionary<Vector2, List<Vector2>> adjacencyMap)
        {
            var visited = new HashSet<Vector2>();
            var contourSegments = new List<List<Vector2>>();

            while (adjacencyMap.Count > 0)
            {
                var contourSegment = GetSingleContourSegment(adjacencyMap, visited);
                contourSegments.Add(contourSegment);
            }

            return contourSegments;
        }

        private static List<Vector2> GetSingleContourSegment(Dictionary<Vector2, List<Vector2>> adjacencyMap, HashSet<Vector2> visited)
        {
            var contourSegment = new List<Vector2>();

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

        private static Vector2 PopConnectedPoint(Dictionary<Vector2, List<Vector2>> adjacencyMap, Vector2 p1)
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

        private List<List<Vector2>> GetContours(List<List<Vector2>> contourSegments)
        {
            var contours = new List<List<Vector2>>();

            while (contourSegments.Count > 0)
            {
                var segment1 = contourSegments.First();
                contourSegments.Remove(segment1);

                var segmentIndex = 0;
                while (segmentIndex < contourSegments.Count)
                {
                    var segment2 = contourSegments[segmentIndex];

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

        private List<List<Vector2>> GetOrientedContours(List<List<Vector2>> contours, float value)
        {
            var orientedContours = new List<List<Vector2>>();

            foreach (var contour in contours)
            {
                var orientedContour = contour;

                // Ensure contours are always returned with a deterministic ordering where lower values
                // are to the left of the vector formed by any two subsequent points in the contour
                if (IsLowerOnLeft(contour, value))
                {
                    orientedContour = ((IEnumerable<Vector2>)contour).Reverse().ToList();
                }

                orientedContours.Add(orientedContour);
            }

            return orientedContours;
        }

        private bool IsLowerOnLeft(List<Vector2> contour, float value)
        {
            var size = Mathf.Max(_values.GetLength(0), _values.GetLength(1));

            for (int i = 0; i < contour.Count - 1; i++)
            {
                var p1 = contour[i];
                var p2 = contour[i + 1];

                var direction = p2 - p1;
                if (direction == Vector2.zero)
                {
                    continue;
                }

                var perpendicular = new Vector2(direction.y, -direction.x).normalized;
                var midpoint = (p1 + p2) / 2f;

                for (int j = 1; j < size; j++)
                {
                    var left = midpoint + perpendicular * j;
                    var right = midpoint - perpendicular * j;

                    var isLeftInBounds = InsideArray(left);
                    var isRightInBounds = InsideArray(right);

                    if (!isLeftInBounds && !isRightInBounds)
                    {
                        break;
                    }

                    if (isLeftInBounds && _values[(int)left.x, (int)left.y] < value)
                    {
                        return true;
                    }
                }
            }

            // Nothing could be determined
            return false;
        }

        private bool InsideArray(Vector2 position)
        {
            int x = (int)position.x;
            int y = (int)position.y;
            return x >= 0 && x < _values.GetLength(0) &&
                   y >= 0 && y < _values.GetLength(1);
        }
    }
}
