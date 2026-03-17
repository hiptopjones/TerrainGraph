namespace CodeFirst.TerrainGraph.Editor
{
    public interface IVersionedObject
    {
        bool IsValid { get; }
        float ExecutionTime { get; set; }
        int VersionHash { get; set; }
    }
}