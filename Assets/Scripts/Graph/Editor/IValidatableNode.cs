using Unity.GraphToolkit.Editor;

internal interface IValidatableNode
{
    void ValidateNode(GraphLogger graphLogger);
}