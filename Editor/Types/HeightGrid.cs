using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class HeightGrid : IVersionedObject
    {
        public int Size;
        public RenderTexture RenderTexture;

        public float[] Values { get; set; }
        public float ExecutionTime { get; set; }
        public int VersionHash { get; set; }

        public bool IsValid => Values != null && Values.Length > 0;

        public float this[int index]
        {
            get => Values[index];
            set => Values[index] = value;
        }

        public float this[int x, int y]
        {
            get => Values[x + y * Size];
            set => Values[x + y * Size] = value;
        }

        public HeightGrid(int size)
        {
            Size = size;
            Values = new float[size * size];
        }

        public float[,] GetHeights()
        {
            var heights = new float[Size, Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    heights[x, y] = Values[x + y * Size];
                }
            }

            return heights;
        }
    }
}
