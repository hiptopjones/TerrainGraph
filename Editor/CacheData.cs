using UnityEngine;

namespace CodeFirst.TerrainGraph.Editor
{
    public class CacheData<T> where T : IVersionedObject
    {
        public T Output;
        public RenderTexture RenderTexture;
        public int PreviewHash;
        public Texture PreviewTexture;
        public int GridSize; // Either size of grid or bounding size for spline
    }
}