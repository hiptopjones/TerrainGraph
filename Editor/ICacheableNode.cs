namespace Indiecat.TerrainGraph.Editor
{
    interface ICacheableNode<T> where T : IVersionedObject
    {
        CacheData<T> CacheData { get; set; }
    }
}