using Unity.GraphToolkit.Editor;
using UnityEditor;
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
            // Make it very clear there is a problem
            previewImage.Texture = (Texture2D)EditorGUIUtility.IconContent("console.warnicon.sml").image;
        }
        else
        {
            previewImage.Texture = TextureHelpers.CreatePreviewTexture(outputGrid);
        }

        PreviewDispatcher.UpdatePreview(previewImage);
    }
}
