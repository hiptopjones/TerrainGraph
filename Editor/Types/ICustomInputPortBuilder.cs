namespace Indiecat.TerrainGraph.Editor
{
    public interface ICustomInputPortBuilder<T>
    {
        ICustomInputPortBuilder<T> WithDisplayName(string name);
        ICustomInputPortBuilder<T> WithDefaultValue(T value);
        ICustomInputPortBuilder<T> WithRange(T min, T max);
    }
}
