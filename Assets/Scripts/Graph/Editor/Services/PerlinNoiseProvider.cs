using System;
using UnityEngine;

public class PerlinNoiseProvider : NoiseProvider
{
    public Vector2 Offset { get; set; }
    public float Frequency { get; set; }
    public float Amplitude { get; set; }
    public int Octaves { get; set; }
    public float Persistence { get; set; }
    public float Lacunarity { get; set; }
    public int Seed { get; set; }

    public override bool IsValid => IsValidParameters();

    public override int VersionHash { get; set; }

    public override float GetNoise(Vector2 position)
    {
        return NoiseHelpers.GeneratePerlinNoise(position + Offset, Frequency, Amplitude, Octaves, Persistence, Lacunarity, Seed);
    }

    public override float[,] GetNoiseArray2D(Vector2 position, int size)
    {
        return NoiseHelpers.GeneratePerlinNoise(size, position + Offset, Frequency, Amplitude, Octaves, Persistence, Lacunarity, Seed);
    }

    private bool IsValidParameters()
    {
        if (Frequency <= 0)
        {
            return false;
        }

        if (Amplitude <= 0)
        {
            return false;
        }

        if (Octaves <= 0)
        {
            return false;
        }

        if (Persistence <= 0)
        {
            return false;
        }

        if (Lacunarity <= 0)
        {
            return false;
        }

        return true;
    }
}
