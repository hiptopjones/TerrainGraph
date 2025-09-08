using Unity.GraphToolkit.Editor;

internal interface IValidatedNode
{
    void ValidateNode(GraphLogger graphLogger);
}