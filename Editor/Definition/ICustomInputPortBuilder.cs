using Unity.GraphToolkit.Editor;

namespace Indiecat.TerrainGraph.Editor
{
    public interface ICustomInputPortBuilder<TPort>
    {
        ICustomInputPortBuilder<TPort> WithDisplayName(string name);
        ICustomInputPortBuilder<TPort> WithDefaultValue(TPort value);
        ICustomInputPortBuilder<TPort> WithRange(TPort min, TPort max);

        IPort Build();
    }
}
