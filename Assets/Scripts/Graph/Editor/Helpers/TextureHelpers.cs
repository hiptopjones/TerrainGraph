using UnityEngine;

internal static class TextureHelpers
{
    public static void ClearTexture(Texture2D texture)
    {
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, new Color(0, 0, 0, 1));
            }
        }

        texture.Apply(false, false);
    }

    public static void UpdateTexture(HeightGrid grid, Texture2D texture)
    {
        if (texture.width != grid.Width ||
            texture.height != grid.Height)
        {
            ClearTexture(texture);
            return;
        }

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, new Color(grid[x, y], 0, 0, 1));
            }
        }

        texture.Apply(false, false);
    }

    public static Texture2D CreateTexture(HeightGrid grid)
    {
        var width = grid.Width;
        var height = grid.Height;

        var texture = new Texture2D(width, height, TextureFormat.R16, mipChain: false, linear: true);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        UpdateTexture(grid, texture);

        return texture;
    }
}
