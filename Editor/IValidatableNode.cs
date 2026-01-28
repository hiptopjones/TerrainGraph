using Unity.GraphToolkit.Editor;

namespace Indiecat.TerrainGraph.Editor
{
    internal interface IValidatableNode
    {
        bool IsNodeValid { get; }
        bool TryValidateNode(GraphLogger graphLogger);
    }
}