using System;
using UnityEngine.Splines;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class SplineWrapper : IVersionedObject
    {
        // TODO: Add custom drawer to show the number of vertices, etc.
        public Spline Spline;

        public int VersionHash { get; set; }
        public float ExecutionTime { get; set; }
        public bool IsValid => Spline != null && Spline.Count > 0;
    }
}
