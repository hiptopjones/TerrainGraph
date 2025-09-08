using Unity.GraphToolkit.Editor;

interface IEvaluatedNode<T>
{
    void ResetNode();
    bool TryGetPortValue(IPort outputPort, out T value);
}