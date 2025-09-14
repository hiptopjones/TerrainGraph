public interface IVersionedData
{
    bool IsValid { get; }
    int VersionHash { get; }
}