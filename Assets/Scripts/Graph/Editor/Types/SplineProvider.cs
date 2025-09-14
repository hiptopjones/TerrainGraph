using UnityEngine.Splines;

public abstract class SplineProvider : IVersionedObject
{
    public abstract bool IsValid { get; }
    public abstract int VersionHash { get; set; }

    public abstract bool TryGetSpline(int vertexCount, out Spline spline);
}