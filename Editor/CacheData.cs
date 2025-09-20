using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    public class CacheData<T> where T : IVersionedObject
    {
        public T Output;
        public int PreviewHash;
        public Texture2D PreviewTexture;
    }
}