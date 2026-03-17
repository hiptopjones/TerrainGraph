using System;
using System.Collections.Generic;
using UnityEngine.Splines;

namespace CodeFirst.TerrainGraph.Editor
{
    [Serializable]
    public class SplineListWrapper : IVersionedObject
    {
        public List<Spline> Splines = new();

        public int VersionHash { get; set; }
        public float ExecutionTime { get; set; }
        public bool IsValid => Splines != null && Splines.Count > 0;
    }
}
