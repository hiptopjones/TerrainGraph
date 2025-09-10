using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PreviewDispatcher
{
    private static Dictionary<PreviewImage, List<Image>> _targetImages = new();
    private static Dictionary<Image, Action> _imageActions = new();
    private static Dictionary<Image, PreviewImage> _imageTargets = new();

    // TODO: This whole thing feels pretty heavy-handed
    public static void Register(PreviewImage target, Image image, Action<PreviewImage, Image> onUpdateTexture)
    {
        if (!_targetImages.TryGetValue(target, out var images))
        {
            images = new List<Image>();
            _targetImages[target] = images;
        }

        images.Add(image);

        Action action = () => onUpdateTexture(target, image);
        _imageActions[image] = action;

        _imageTargets[image] = target;

        // This cleans up the references when the UI goes away
        image.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
    }

    private static void OnDetachFromPanel(DetachFromPanelEvent e)
    {
        var image = e.currentTarget as Image;

        if (!_imageTargets.TryGetValue(image, out var target))
        {
            return;
        }

        _imageActions.Remove(image);
        _imageTargets.Remove(image);
        _targetImages[target].Remove(image);
    }

    public static void UpdatePreview(PreviewImage target)
    {
        if (_targetImages.TryGetValue(target, out var images))
        {
            foreach (var image in images)
            {
                if (_imageActions.TryGetValue(image, out var action))
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
