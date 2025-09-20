using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    public class PerlinNoiseProvider : IHeightProvider, INoiseProvider
    {
        public Vector2 Offset { get; set; }
        public float Frequency { get; set; }
        public float Amplitude { get; set; }
        public int Octaves { get; set; }
        public float Persistence { get; set; }
        public float Lacunarity { get; set; }
        public int Seed { get; set; }

        public bool IsValid => true;
        public float ExecutionTime => 0;
        public int VersionHash { get; set; }

        public bool TryGetHeights(int size, out float[,] heights)
        {
            heights = NoiseHelpers.GeneratePerlinNoise(size, Offset, Frequency, Amplitude, Octaves, Persistence, Lacunarity, Seed);
            return true;
        }

        public bool TryGetNoise(Vector2 position, out float noise)
        {
            noise = NoiseHelpers.GeneratePerlinNoise(position + Offset, Frequency, Amplitude, Octaves, Persistence, Lacunarity, Seed);
            return true;
        }
    }
}
