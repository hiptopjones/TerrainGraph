using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PreviewDispatcher
{
    private static Dictionary<PreviewImage, List<Image>> targetImages = new();
    private static Dictionary<Image, Action> imageActions = new();
    private static Dictionary<Image, PreviewImage> imageTargets = new();

    // TODO: This whole thing feels pretty heavy-handed
    public static void Register(PreviewImage target, Image image, Action<PreviewImage, Image> onUpdateTexture)
    {
        if (!targetImages.TryGetValue(target, out var images))
        {
            images = new List<Image>();
            targetImages[target] = images;
        }

        images.Add(image);

        Action action = () => onUpdateTexture(target, image);
        imageActions[image] = action;

        imageTargets[image] = target;

        // This cleans up the references when the UI goes away
        image.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
    }

    private static void OnDetachFromPanel(DetachFromPanelEvent e)
    {
        var image = e.currentTarget as Image;

        if (!imageTargets.TryGetValue(image, out var target))
        {
            return;
        }

        imageActions.Remove(image);
        imageTargets.Remove(image);
        targetImages[target].Remove(image);
    }

    public static void UpdatePreview(PreviewImage target)
    {
        if (targetImages.TryGetValue(target, out var images))
        {
            foreach (var image in images)
            {
                if (imageActions.TryGetValue(image, out var action))
                {
                    action.Invoke();
                }
                else
                {
                    Debug.Log($"no action found for target: {target}");
                }
            }
        }
    }
}
