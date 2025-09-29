using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class TerrainDataWrapper : IVersionedObject
    {
        public TerrainData TerrainData;

        public int VersionHash { get; set; }
        public float ExecutionTime { get; set; }
        public bool IsValid => TerrainData != null;
    }
}
