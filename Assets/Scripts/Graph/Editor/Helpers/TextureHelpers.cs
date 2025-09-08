using UnityEngine;

internal static class TextureHelpers
{
    public static void UpdateTexture(float[,] heights, Texture2D texture)
    {
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, new Color(heights[x, y], 0, 0, 1));
            }
        }

        texture.Apply(false, false);
    }

    public static Texture2D CreateTexture(float[,] heights)
    {
        var width = heights.GetLength(0);
        var height = heights.GetLength(1);

        var texture = new Texture2D(width, height, TextureFormat.R16, mipChain: false, linear: true);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        UpdateTexture(heights, texture);

        return texture;
    }
}
