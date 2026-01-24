using Unity.GraphToolkit.Editor;

namespace Indiecat.TerrainGraph.Editor
{
    public interface ICustomOptionBuilder<TOption>
    {
        ICustomOptionBuilder<TOption> WithDisplayName(string displayName);
        ICustomOptionBuilder<TOption> WithDefaultValue(TOption defaultValue);
        ICustomOptionBuilder<TOption> WithRange(TOption min, TOption max);

        INodeOption Build();
    }
}
