using System;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class NullOutput : IVersionedObject
    {
        public float ExecutionTime { get; set; }
        public int VersionHash { get; set; }

        public bool IsValid => false;

    }
}
