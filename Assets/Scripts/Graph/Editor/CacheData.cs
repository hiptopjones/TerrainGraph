using UnityEngine;

public class CacheData<T> where T : IVersionedData
{
    public T Output;
    public int PreviewHash;
    public Texture2D PreviewTexture;
}