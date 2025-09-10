using Unity.GraphToolkit.Editor;

internal static class PreviewHelpers
{
    public static void UpdatePreview(INode node, string previewPortId, HeightGrid outputGrid)
    {
        var previewPort = node.GetInputPortByName(previewPortId);
        previewPort.TryGetValue(out PreviewImage previewImage);

        if (outputGrid == null)
        {
            if (previewImage.Texture != null)
            {
                TextureHelpers.ClearTexture(previewImage.Texture);
            }

            return;
        }

        if (previewImage.Texture == null)
        {
            previewImage.Texture = TextureHelpers.CreateTexture(outputGrid);
        }
        else
        {
            TextureHelpers.UpdateTexture(outputGrid, previewImage.Texture);
        }
    }
}
