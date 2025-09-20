using UnityEngine;
using Random = UnityEngine.Random;

namespace Indiecat.TerrainGraph.Editor
{
    public static class NoiseHelpers
    {
        public static float GeneratePerlinNoise(Vector2 position, float frequency, float amplitude, int octaves, float persistence, float lacunarity, int seed = 0)
        {
            position = GetOffsetPositionInternal(position, seed);
            return GeneratePerlinNoiseInternal(position, frequency, amplitude, octaves, persistence, lacunarity);
        }

        public static float[,] GeneratePerlinNoise(int size, Vector2 start, float frequency, float amplitude, int octaves, float persistence, float lacunarity, int seed = 0)
        {
            start = GetOffsetPositionInternal(start, seed);

            var noise = new float[size, size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 position = start + new Vector2(x, y);

                    noise[x, y] = GeneratePerlinNoiseInternal(position, frequency, amplitude, octaves, persistence, lacunarity);
                }
            }

            return noise;
        }

        private static Vector2 GetOffsetPositionInternal(Vector2 position, int seed)
        {
            Random.InitState(seed);

            var offsetX = Random.value * 200000 - 100000;
            var offsetY = Random.value * 200000 - 100000;

            // Offset the sampling position by a repeatable random location based on a seed
            position += new Vector2(offsetX, offsetY);

            return position;
        }

        private static float GeneratePerlinNoiseInternal(Vector2 position, float frequency, float amplitude, int octaves, float persistence, float lacunarity)
        {
            var totalNoise = 0f;
            var currentAmplitude = amplitude;
            var totalAmplitude = 0f;
            var currentFreqency = frequency;

            for (int i = 0; i < octaves; i++)
            {
                totalNoise += Mathf.PerlinNoise(position.x * frequency, position.y * frequency) * currentAmplitude;

                totalAmplitude += currentAmplitude;
                currentAmplitude *= persistence;
                currentFreqency *= lacunarity;
            }

            return Mathf.Clamp01(totalNoise / totalAmplitude);
        }

        // Returns a "cell noise" value for a given world position
        public static float GetCellHeight(Vector2Int cellIndex, int seed = 0)
        {
            // Get deterministic random height for this cell
            int rand = HashHelpers.S32(cellIndex.x, cellIndex.y, seed);

            // Scale to desired range
            return Mathf.Clamp01(Mathf.Abs(rand / (float)int.MaxValue));
        }


        // Sample noise in a circle through the noise field, which makes it seamless
        public static bool TryGetSeamlessNoise(INoiseProvider noiseProvider, float radius, float t, out float noise)
        {
            var x = radius * Mathf.Cos(t * 2 * Mathf.PI);
            var y = radius * Mathf.Sin(t * 2 * Mathf.PI);

            return noiseProvider.TryGetNoise(new Vector2(x, y), out noise);
        }
    }
}