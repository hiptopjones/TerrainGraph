using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Indiecat.TerrainGraph.Editor
{
    public static class NoiseHelpers
    {
        // NOTE: Returns value between -1 and 1
        public static float PerlinNoise2D(float x, float y)
        {
            return PerlinNoise2D(new float2(x, y));
        }

        // NOTE: Returns value between -1 and 1
        public static float PerlinNoise2D(Vector2 p)
        {
            return PerlinNoise2D((float2)p);
        }

        // Matches implementation in compute shader
        // NOTE: Returns value between -1 and 1
        public static float PerlinNoise2D(float2 p)
        {
            float2 i = math.floor(p);
            float2 f = math.frac(p);

            // Four corners in 2D
            float2 u = f * f * (3.0f - 2.0f * f);

            float2 g00 = Hash2D(i + new float2(0.0f, 0.0f));
            float2 g10 = Hash2D(i + new float2(1.0f, 0.0f));
            float2 g01 = Hash2D(i + new float2(0.0f, 1.0f));
            float2 g11 = Hash2D(i + new float2(1.0f, 1.0f));

            float n00 = math.dot(g00, f - new float2(0.0f, 0.0f));
            float n10 = math.dot(g10, f - new float2(1.0f, 0.0f));
            float n01 = math.dot(g01, f - new float2(0.0f, 1.0f));
            float n11 = math.dot(g11, f - new float2(1.0f, 1.0f));

            float nx0 = math.lerp(n00, n10, u.x);
            float nx1 = math.lerp(n01, n11, u.x);
            float nxy = math.lerp(nx0, nx1, u.y);

            return nxy;
        }

        public static float PerlinFBM2D(float x, float y)
        {
            return PerlinFBM2D(new float2(x, y));
        }

        // NOTE: Returns value between -1 and 1
        public static float PerlinFBM2D(Vector2 p)
        {
            return PerlinFBM2D((float2)p);
        }

        // Matches implementation in compute shader
        // NOTE: Returns value between -1 and 1
        public static float PerlinFBM2D(float2 p)
        {
            float sum = 0.0f;

            sum += 0.5f * PerlinNoise2D(p);
            sum += 0.25f * PerlinNoise2D(p * 0.5f + 17f);
            sum += 0.125f * PerlinNoise2D(p * 0.25f + 43f);

            return sum;
        }

        // Hash function (value noise style gradient selection)
        private static float2 Hash2D(float2 p)
        {
            p = new float2(
                math.dot(p, new float2(127.1f, 311.7f)),
                math.dot(p, new float2(269.5f, 183.3f)));

            return -1.0f + 2.0f * math.frac(math.sin(p) * 43758.5453123f);
        }

        public static Vector2 GetOffsetPositionInternal(Vector2 position, int seed)
        {
            Random.InitState(seed);

            var offsetX = Random.value * 200000 - 100000;
            var offsetY = Random.value * 200000 - 100000;

            // Offset the sampling position by a repeatable random location based on a seed
            position += new Vector2(offsetX, offsetY);

            return position;
        }
    }
}