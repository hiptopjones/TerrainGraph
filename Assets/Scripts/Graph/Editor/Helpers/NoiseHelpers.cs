using UnityEngine;
using Random = UnityEngine.Random;

internal static class NoiseHelpers
{
    public static float[,] GeneratePerlinNoise(int size, Vector2 start, float frequency, float amplitude, int octaves, float persistence, float lacunarity, int seed = 0)
    {
        Random.InitState(seed);

        var randomX = Random.value * 200000 - 100000;
        var  randomY = Random.value * 200000 - 100000;

        // Move the starting point to a repeatable random location based on a seed
        start += new Vector2(randomX, randomY);

        var noise = new float[size, size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 sample = start + new Vector2(x, y);

                var totalNoise = 0f;
                var currentAmplitude = amplitude;
                var totalAmplitude = 0f;
                var currentFreqency = frequency;

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