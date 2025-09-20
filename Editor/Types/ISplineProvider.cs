using UnityEngine.Splines;

namespace Indiecat.TerrainGraph.Editor
{
    public interface ISplineProvider : IProvider
    {
        bool TryGetSpline(int vertexCount, out Spline spline);
    }
}