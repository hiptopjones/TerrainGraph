using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    public class CacheData<T> where T : IVersionedObject
    {
        public T Output;
        public RenderTexture RenderTexture;
        public int PreviewHash;
        public Texture PreviewTexture;
    }
}