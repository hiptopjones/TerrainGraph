using UnityEngine;
using UnityEngine.Splines;

namespace Indiecat.TerrainGraph.Editor
{
    public class CircleSplineProvider : ISplineProvider
    {
        public int Size { get; set; }
        public float Angle { get; set; }

        public bool IsValid => true;
        public float ExecutionTime => 0;
        public int VersionHash { get; set; }

        public bool TryGetSpline(int vertexCount, out Spline spline)
        {
            var radius = Size / 2f;
            var center = Vector2.one * radius;
            var interval = Angle / vertexCount;
        
            spline = SplineFunctions.Circle(radius, Angle, interval, center);
            return true;
        }
    }
}
