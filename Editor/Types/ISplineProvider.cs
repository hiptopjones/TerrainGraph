using UnityEngine.Splines;

public interface ISplineProvider : IProvider
{
    bool TryGetSpline(int vertexCount, out Spline spline);
}