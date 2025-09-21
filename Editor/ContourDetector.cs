using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    public class ContourDetector
    {
        private float[,] _values;

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
            var samples = GetSampleGrid(level);
            var segments = GetLineSegments(samples, level);
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
                }
            }

            return samples;
        }

        private List<Vector2> GetLineSegments(byte[,] samples, float level)
        {
            var segments = new List<Vector2>();

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

                        segments.Add(p1);
                        segments.Add(p2);
                    }

                }
            }

            return segments;
        }

        private List<List<Vector2>> GetContours(List<Vector2> segments, float value)
        {
            var connectedPoints = GetConnectedPoints(segments);

            // Turn the point connections into ordered lists of points
            var contours = AssembleContours(connectedPoints, value);

            return contours;
        }

        private Dictionary<Vector2, Vector2> GetConnectedPoints(List<Vector2> segments)
        {
            // The contours are currently a set of unordered line segments.  It would draw fine,
            // but not be useful for much else. So we orient those segments and make a sparse
            // linked list (using a dictionary) of points.
            var connectedPoints = new Dictionary<Vector2, Vector2>();

            for (int i = 0; i < segments.Count; i += 2)
            {
                var p1 = segments[i];
                var p2 = segments[i + 1];

                if (connectedPoints.TryGetValue(p1, out var p) && p2 == p)
                {
                    throw new Exception($"Duplicate entry - contour not valid");
                }

                AddConnection(connectedPoints, p1, p2);
            }

            return connectedPoints;
        }

        private void AddConnection(Dictionary<Vector2, Vector2> connections, Vector2 p1, Vector2 p2)
        {
            Vector2 p3;

            while (true)
            {
                // Check if p1 already has a mapping
                bool collision = connections.TryGetValue(p1, out p3);
                connections[p1] = p2;

                // If no collision, we're good
                if (!collision)
                {
                    return;
                }

                // If there was a collision, we cascade to make room
                p2 = p1;
                p1 = p3;
            }
        }

        private List<List<Vector2>> AssembleContours(Dictionary<Vector2, Vector2> connections, float value)
        {
            var contours = new List<List<Vector2>>();

            // Loop until we have enumerated all contours
            while (connections.Any())
            {
                var visited = new HashSet<Vector2>();
                var contour = new List<Vector2>();

                var p1 = connections.Keys.First();

                while (true)
                {
                    contour.Add(p1);
                    visited.Add(p1);

                    if (!connections.TryGetValue(p1, out Vector2 p2))
                    {
                        // Reached the end of a non-closed contour
                        break;
                    }

                    // Pare down the dictionary as we go
                    connections.Remove(p1);

                    if (visited.Contains(p2))
                    {
                        // We're back at the beginning of the sequence
                        contour.Add(p2);
                        break;
                    }

                    // Setup for next iteration
                    p1 = p2;
                }

                // Ensure contours are always returned with a deterministic ordering where lower values
                // are to the left of the vector formed by any two subsequent points in the contour
                if (IsLowerOnLeft(contour, value))
                {
                    contour = ((IEnumerable<Vector2>)contour).Reverse().ToList();
                }

                contours.Add(contour);
            }

            return contours;
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
