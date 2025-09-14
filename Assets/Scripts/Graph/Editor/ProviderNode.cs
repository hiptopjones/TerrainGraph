using Unity.GraphToolkit.Editor;

public abstract class ProviderNode<T> : Node,
    IValidatableNode,
    IEvaluatableNode<T>
    where T : IVersionedData
{
    public abstract bool TryGetOutputValue(IPort outputPort, out T value);
    public abstract bool TryValidateNode(GraphLogger graphLogger);
}
