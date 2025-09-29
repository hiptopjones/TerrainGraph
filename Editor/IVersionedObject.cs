namespace Indiecat.TerrainGraph.Editor
{
    public interface IVersionedObject
    {
        bool IsValid { get; }
        float ExecutionTime { get; }
        int VersionHash { get; }
    }
}