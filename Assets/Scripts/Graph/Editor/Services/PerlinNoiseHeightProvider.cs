using UnityEngine;

public class PerlinNoiseHeightProvider : HeightProvider
{
    public Vector2 Offset { get; set; }
    public float Frequency { get; set; }
    public float Amplitude { get; set; }
    public int Octaves { get; set; }
    public float Persistence { get; set; }
    public float Lacunarity { get; set; }
    public int Seed { get; set; }

    public override bool IsValid => true;
    public override int VersionHash { get; set; }

    public override bool TryGetHeights(int size, out float[,] heights)
    {
        heights = NoiseHelpers.GeneratePerlinNoise(size, Offset, Frequency, Amplitude, Octaves, Persistence, Lacunarity, Seed);
        return true;
    }
}
