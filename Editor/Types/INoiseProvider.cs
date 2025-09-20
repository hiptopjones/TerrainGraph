using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    public interface INoiseProvider : IProvider
    {
        bool TryGetNoise(Vector2 position, out float noise);
    }
}