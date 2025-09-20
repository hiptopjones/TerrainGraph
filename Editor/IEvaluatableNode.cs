using Unity.GraphToolkit.Editor;

namespace Indiecat.TerrainGraph.Editor
{
    interface IEvaluatableNode<T>
    {
        bool TryGetOutputValue(IPort outputPort, out T value);
    }
}