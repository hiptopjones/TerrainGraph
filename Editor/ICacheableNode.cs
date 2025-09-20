interface ICacheableNode<T> where T : IVersionedObject
{
    CacheData<T> CacheData { get; set; }
}