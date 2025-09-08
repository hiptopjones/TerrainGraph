using UnityEngine;
using Random = UnityEngine.Random;

internal static class NoiseHelpers
{
    public static float[,] GeneratePerlinNoise(int size, Vector2 start, float frequency, float amplitude, int octaves, float persistence, float lacunarity, int seed = 0)
    {
        Random.InitState(seed);

        float randomX = Random.value * 200000 - 100000;
        float randomY = Random.value * 200000 - 100000;

        // Move the starting point to a repeatable random location based on a seed
        start += new Vector2(randomX, randomY);

        float[,] noise = new float[size, size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 sample = start + new Vector2(x, y);

                float totalNoise = 0;
                float currentAmplitude = amplitude;
                float totalAmplitude = 0;
                float currentFreqency = frequency;

                for (int i = 0; i < octaves; i++)
                {
                    totalNoise += Mathf.PerlinNoise(sample.x * frequency, sample.y * frequency) * currentAmplitude;

                    totalAmplitude += currentAmplitude;
                    currentAmplitude *= persistence;
                    currentFreqency *= lacunarity;
                }

                noise[x, y] = totalNoise / totalAmplitude;
            }
        }

        return noise;
    }
}