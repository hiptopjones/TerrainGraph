using Unity.GraphToolkit.Editor;

internal interface IValidatableNode
{
    bool TryValidateNode(GraphLogger graphLogger);
}