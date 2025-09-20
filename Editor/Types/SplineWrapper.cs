using System;
using UnityEngine.Splines;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class SplineWrapper : IVersionedObject
    {
        public Spline Spline;

        public int VersionHash { get; set; }
        public float ExecutionTime { get; set; }
        public bool IsValid => Spline != null && Spline.Count > 0;
    }
}
