using System;
using UnityEngine.Splines;

[Serializable]
public class SplineWrapper : IVersionedObject
{
    public int Size; // Should match max of Spline.Bounds
    public Spline Spline;

    // TODO: Create a common pattern and wrapper for expensive, computable objects
    public int VersionHash { get; set; }

    public bool IsValid => Spline != null && Spline.Count > 0;
}
