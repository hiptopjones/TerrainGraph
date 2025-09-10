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

    public static Texture2D CreateHeightMapTexture(HeightGrid grid)
    {
        var width = grid.Width;
        var height = grid.Height;

        var texture = new Texture2D(width, height, TextureFormat.R16, mipChain: false, linear: true);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, new Color(grid[x, y], 0, 0, 1));
            }
        }

        texture.Apply(false, false);

        return texture;
    }

    public static Texture2D CreatePreviewTexture(HeightGrid grid)
    {
        var width = grid.Width;
        var height = grid.Height;

        var texture = new Texture2D(width, height, TextureFormat.RGB24, mipChain: false, linear: true);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                var value = grid[x, y];

                if (value > 1)
                {
                    texture.SetPixel(x, y, Color.green);
                }
                else if (value < 0)
                {
                    texture.SetPixel(x, y, Color.red);
                }
                else
                {
                    texture.SetPixel(x, y, new Color(value, value, value));
                }
            }
        }

        texture.Apply(false, false);

        return texture;
    }
}
