using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    public class CellularNoiseProvider : IHeightProvider, INoiseProvider
    {
        public Vector2 Offset { get; set; }
        public int CellSize { get; set; }
        public float RadiusPercent { get; set; } // TODO: This should be a bounds provider or something
        public int Seed { get; set; }

        public bool IsValid => true;
        public float ExecutionTime => 0;
        public int VersionHash { get; set; }

        public bool TryGetHeights(int size, out float[,] heights)
        {
            heights = new float[size, size];

            var radius = RadiusPercent * size;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var position = new Vector2(x, y);

                    var offsetFraction = new Vector2(Offset.x % CellSize, Offset.y % CellSize);
                    var localCellIndex = GetCellIndex(position + offsetFraction);

                    var center = new Vector2(size / 2f, size / 2f);
                    var cellCenter = (localCellIndex + Vector2.one * 0.5f) * CellSize - offsetFraction;

                    if ((cellCenter - center).magnitude > radius)
                    {
                        // Cell not included
                        heights[x, y] = 0;
                    }
                    else
                    {
                        var cellIndex = GetCellIndex(position + Offset);
                        heights[x, y] = NoiseHelpers.GetCellHeight(cellIndex, Seed);
                    }

                    // Debugging the radius
                    //if (Mathf.Abs((position - center).magnitude - radius) < 1)
                    //{
                    //    heights[x, y] = 1;
                    //}
                }
            }
            return true;
        }

        public bool TryGetNoise(Vector2 position, out float noise)
        {
            var cellIndex = GetCellIndex(position + Offset);

            noise = NoiseHelpers.GetCellHeight(cellIndex, Seed);
            return true;
        }

        private Vector2Int GetCellIndex(Vector2 position)
        {
            // Which cell are we in?
            int cellX = Mathf.FloorToInt(position.x / CellSize);
            int cellY = Mathf.FloorToInt(position.y / CellSize);

            return new Vector2Int(cellX, cellY);
        }
    }
}
