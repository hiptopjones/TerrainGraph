using Unity.GraphToolkit.Editor;

interface IEvaluatableNode<T>
{
    void ResetNode(int generationId);
    bool TryGetPortValue(IPort outputPort, int generationId, out T value);
}