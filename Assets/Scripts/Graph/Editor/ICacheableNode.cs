interface ICacheableNode<T> where T : IVersionedData
{
    CacheData<T> CacheData { get; set; }
}