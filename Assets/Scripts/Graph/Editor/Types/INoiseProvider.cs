using UnityEngine;

public interface INoiseProvider : IProvider
{
    bool TryGetNoise(Vector2 position, out float noise);
}