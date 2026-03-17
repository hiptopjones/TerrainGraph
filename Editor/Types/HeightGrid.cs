using System;
using UnityEngine;

namespace CodeFirst.TerrainGraph.Editor
{
    [Serializable]
    public class HeightGrid : IVersionedObject
    {
        public int Size;

        public RenderTexture RenderTexture { get; set; }

        public float ExecutionTime { get; set; }
        public int VersionHash { get; set; }

        public bool IsValid => RenderTexture != null;

        public HeightGrid(int size)
        {
            Size = size;
        }
    }
}
