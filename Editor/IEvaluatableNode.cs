using Unity.GraphToolkit.Editor;

namespace CodeFirst.TerrainGraph.Editor
{
    interface IEvaluatableNode<T>
    {
        bool TryGetOutputValue(IPort outputPort, out T value);
    }
}