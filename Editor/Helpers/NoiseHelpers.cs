using UnityEngine;
using Random = UnityEngine.Random;

namespace Indiecat.TerrainGraph.Editor
{
    public static class NoiseHelpers
    {
        public static Vector2 GetOffsetPositionInternal(Vector2 position, int seed)
        {
            Random.InitState(seed);

            var offsetX = Random.value * 200000 - 100000;
            var offsetY = Random.value * 200000 - 100000;

            // Offset the sampling position by a repeatable random location based on a seed
            position += new Vector2(offsetX, offsetY);

            return position;
        }

        //// Sample noise in a circle through the noise field, which makes it seamless
        //public static bool TryGetSeamlessNoise(INoiseProvider noiseProvider, float radius, float t, out float noise)
        //{
        //    var x = radius * Mathf.Cos(t * 2 * Mathf.PI);
        //    var y = radius * Mathf.Sin(t * 2 * Mathf.PI);

        //    return noiseProvider.TryGetNoise(new Vector2(x, y), out noise);
        //}
    }
}