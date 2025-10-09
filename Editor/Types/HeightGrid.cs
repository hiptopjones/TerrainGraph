using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class HeightGrid : IVersionedObject
    {
        public int Size;
        public RenderTexture RenderTexture;

        public float ExecutionTime { get; set; }
        public int VersionHash { get; set; }

        public bool IsValid => RenderTexture != null;

        public HeightGrid(int size)
        {
            Size = size;
        }
    }
}
