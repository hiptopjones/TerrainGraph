namespace Indiecat.TerrainGraph.Editor
{
    public interface ICustomOutputPortBuilder<T>
    {
        ICustomOutputPortBuilder<T> WithDisplayName(string name);
    }
}
