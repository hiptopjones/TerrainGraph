using Unity.GraphToolkit.Editor;

interface IEvaluatableNode<T>
{
    bool TryGetOutputValue(IPort outputPort, out T value);
}