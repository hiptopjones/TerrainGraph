using Unity.GraphToolkit.Editor;

internal static class PreviewHelpers
{
    public static void UpdatePreview(INode node, string previewPortId, string sourcePortId, int generationId)
    {
        var previewPort = node.GetInputPortByName(previewPortId);
        previewPort.TryGetValue(out PreviewImage previewImage);

        var evaluatableNode = node as IEvaluatableNode<float[,]>;

        var outputPort = node.GetOutputPortByName(sourcePortId);
        if (!evaluatableNode.TryGetPortValue(outputPort, generationId, out var value))
        {
            if (previewImage.Texture != null)
            {
                TextureHelpers.ClearTexture(previewImage.Texture);
            }

            return;
        }

        if (previewImage.Texture == null)
        {
            previewImage.Texture = TextureHelpers.CreateTexture(value);
        }
        else
        {
            TextureHelpers.UpdateTexture(value, previewImage.Texture);
        }
    }
}
