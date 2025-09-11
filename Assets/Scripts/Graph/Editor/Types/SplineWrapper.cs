using System;
using UnityEngine.Splines;

[Serializable]
public class SplineWrapper
{
    public int Size;
    public Spline Spline;

    // TODO: Create a common pattern and wrapper for expensive, computable objects
    public int GenerationHash;
}
