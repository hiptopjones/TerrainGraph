using Unity.GraphToolkit.Editor;

namespace Indiecat.TerrainGraph.Editor
{
    internal interface IValidatableNode
    {
        bool TryValidateNode(GraphLogger graphLogger);
    }
}