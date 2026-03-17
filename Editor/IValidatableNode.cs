using Unity.GraphToolkit.Editor;

namespace CodeFirst.TerrainGraph.Editor
{
    internal interface IValidatableNode
    {
        bool TryValidateNode(GraphLogger graphLogger);
    }
}