public interface IVersionedObject
{
    bool IsValid { get; }
    int VersionHash { get; }
}