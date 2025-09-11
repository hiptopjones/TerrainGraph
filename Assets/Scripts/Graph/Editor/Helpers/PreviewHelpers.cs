using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

internal static class PreviewHelpers
{
    public static bool TryUpdatePreview(INode node, string previewPortId, HeightGrid outputGrid)
    {
        try
        {
            var previewPort = node.GetInputPortByName(previewPortId);
            if (!previewPort.TryGetValue(out PreviewImage previewImage))
            {
                Debug.LogError("Preview image not found");
                return false;
            }

            if (outputGrid == null || outputGrid.Values == null || outputGrid.Values.Length == 0)
            {
                // Make it very clear there is a problem
                previewImage.Texture = (Texture2D)EditorGUIUtility.IconContent("console.warnicon.sml").image;
            }
            else
            {
                previewImage.Texture = TextureHelpers.CreatePreviewTexture(outputGrid);
            }

            PreviewDispatcher.UpdatePreview(previewImage);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
    }

    public static bool TryUpdatePreview(INode node, string previewPortId, SplineWrapper outputSpline)
    {
        try
        {
            var previewPort = node.GetInputPortByName(previewPortId);
            if (!previewPort.TryGetValue(out PreviewImage previewImage))
            {
                Debug.LogError("Preview image not found");
                return false;
            }

            if (outputSpline == null || outputSpline.Spline == null || outputSpline.Spline.Count == 0)
            {
                // Make it very clear there is a problem
                previewImage.Texture = (Texture2D)EditorGUIUtility.IconContent("console.warnicon.sml").image;
            }
            else
            {
                previewImage.Texture = TextureHelpers.CreatePreviewTexture(outputSpline);
            }

            PreviewDispatcher.UpdatePreview(previewImage);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
    }
}
