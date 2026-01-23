using Unity.GraphToolkit.Editor;

namespace Indiecat.TerrainGraph.Editor
{
    public interface ICustomOptionBuilder<T>
    {
        ICustomOptionBuilder<T> WithDisplayName(string displayName);
        ICustomOptionBuilder<T> WithDefaultValue(T defaultValue);
        ICustomOptionBuilder<T> WithRange(T min, T max);

        INodeOption Build();
    }
}
