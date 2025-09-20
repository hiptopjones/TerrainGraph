using Unity.GraphToolkit.Editor;

namespace Indiecat.TerrainGraph.Editor
{
    public abstract class ProviderNode<T> : Node,
        IValidatableNode,
        IEvaluatableNode<T>
        where T : IVersionedObject
    {
        public abstract bool TryGetOutputValue(IPort outputPort, out T value);
        public abstract bool TryValidateNode(GraphLogger graphLogger);
    }
}
