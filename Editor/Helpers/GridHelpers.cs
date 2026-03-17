using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CodeFirst.TerrainGraph.Editor
{
    public static class GridHelpers
    {
        public static float SafeIndex(float[,] heights, int x, int y)
        {
            int size = heights.GetLength(0);

            x = Mathf.Clamp(x, 0, size - 1);
            y = Mathf.Clamp(y, 0, size - 1);

            return heights[x, y];
        }


        public static float SafeIndex(float[,] heights, float x, float y)
        {
            var x1 = Mathf.FloorToInt(x);
            var y1 = Mathf.FloorToInt(y);
            var x2 = Mathf.FloorToInt(x + 1);
            var y2 = Mathf.FloorToInt(y + 1);

            var q11 = SafeIndex(heights, x1, y1);
            var q21 = SafeIndex(heights, x2, y1);
            var q22 = SafeIndex(heights, x2, y2);
            var q12 = SafeIndex(heights, x1, y2);

            var height = GeometryHelpers.BilinearInterpolate(x, y, q11, q21, q22, q12, x1, y1, x2, y2);
            return height;
        }

        public static (float, float) GetRange(float[,] heights)
        {
            var size = heights.GetLength(0);

            var maxValue = float.MinValue;
            var minValue = float.MaxValue;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var value = heights[x, y];

                    maxValue = Mathf.Max(maxValue, value);
                    minValue = Mathf.Min(minValue, value);
                }
            }

            return (minValue, maxValue);
        }

        public static List<List<Vector2Int>> GetClusters(float[,] heights)
        {
            var neighbors = new Vector2Int[]
            {
                new Vector2Int(-1, 0),
                new Vector2Int(1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, -1),
            };

            var clusters = new List<List<Vector2Int>>();
            var visited = new HashSet<Vector2Int>();

            var size = heights.GetLength(0);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2Int(x, y);

                    if (visited.Add(p))
                    {
                        if (heights[p.x, p.y] != 0)
                        {
                            var cluster = new List<Vector2Int>();
                            var queue = new Queue<Vector2Int>();

                            queue.Enqueue(p);

                            while (queue.Any())
                            {
                                var q = queue.Dequeue();

                                cluster.Add(q);

                                foreach (var neighbor in neighbors)
                                {
                                    var n = q + neighbor;

                                    if (n.x >= 0 && n.x < size &&
                                        n.y >= 0 && n.y < size)
                                    {
                                        if (visited.Add(n))
                                        {
                                            if (heights[n.x, n.y] != 0)
                                            {
                                                queue.Enqueue(n);
                                            }
                                        }
                                    }
                                }
                            }

                            clusters.Add(cluster);
                        }
                    }
                }
            }

            return clusters;
        }

        public static void CopyHeights(float[] tempHeights, float[,] heights)
        {
            var size = heights.GetLength(0);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    heights[x, y] = tempHeights[x + y * size];
                }
            }
        }
    }
}
