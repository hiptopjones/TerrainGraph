using Unity.GraphToolkit.Editor;

public interface IVersionedData
{
    bool IsValid { get; }
    int VersionHash { get; }
}