using Unity.GraphToolkit.Editor;
using UnityEngine;

internal static class PreviewHelpers
{
    public static void UpdatePreview(INode node, string previewPortId, HeightGrid outputGrid)
    {
        var previewPort = node.GetInputPortByName(previewPortId);
        if (!previewPort.TryGetValue(out PreviewImage previewImage))
        {
            Debug.Log("preview image not found");
            return;
        }

        if (outputGrid == null || outputGrid.Values.Length == 0)
        {
            previewImage.Texture = null;
        }
        else
        {
            previewImage.Texture = TextureHelpers.CreateTexture(outputGrid);
        }

        PreviewDispatcher.UpdatePreview(previewImage);
    }
}
