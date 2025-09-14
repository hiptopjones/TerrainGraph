using System;
using UnityEngine;

public class TextureHeightProvider : HeightProvider
{
    public Texture2D Texture { get; set; }

    public override bool IsValid => true;
    public override int VersionHash { get; set; }

    public override bool TryGetHeights(int size, out float[,] heights)
    {
        try
        {
            var inputSize = new Vector2Int(Texture.width, Texture.height);
            var outputSize = size;

            // TODO: Could have control over alignment of non-square textures
            var outputCenter = Vector2Int.one * outputSize / 2;
            var inputCenter = Vector2Int.one * inputSize / 2;

            var output = new float[size, size];
            for (int y = 0; y < outputSize; y++)
            {
                for (int x = 0; x < outputSize; x++)
                {
                    var target = new Vector2Int(x, y);
                    var source = target - outputCenter + inputCenter;

                    if (source.x < 0 || source.x > inputSize.x - 1 ||
                        source.y < 0 || source.y > inputSize.y - 1)
                    {
                        output[x, y] = 0;
                    }
                    else
                    {
                        var color = Texture.GetPixel(source.x, source.y);
                        output[x, y] = color.grayscale;
                    }
                }
            }

            heights = output;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);

            heights = null;
            return false;
        }
    }
}
