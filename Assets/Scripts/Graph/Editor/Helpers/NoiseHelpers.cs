using UnityEngine;
using Random = UnityEngine.Random;

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
}