using System;
using UnityEngine;

public abstract class NoiseProvider : IVersionedData
{
    public abstract bool IsValid { get; }
    public abstract int VersionHash { get; set; }

    public abstract float GetNoise(Vector2 position);
    public abstract float[,] GetNoiseArray2D(Vector2 start, int size);
}