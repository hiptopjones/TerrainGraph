using System.Collections.Generic;
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

                Color color;
                if (value > 1)
                {
                    color = Color.green;
                }
                else if (value < 0)
                {
                    color = Color.red;
                }
                else
                {
                    color = new Color(value, value, value);
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply(false, false);

        return texture;
    }

    // TODO: Instead, use the spline-to-mask conversion, and then reuse the above method
    public static Texture2D CreatePreviewTexture(SplineWrapper spline)
    {
        var width = spline.Size;
        var height = spline.Size;

        var texture = new Texture2D(width, height, TextureFormat.RGB24, mipChain: false, linear: true);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        // Clear the texture
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, Color.black);
            }
        }

        // Draw the spline points
        foreach (var point in spline.Spline)
        {
            var x = Mathf.RoundToInt(point.Position.x);
            var y = Mathf.RoundToInt(point.Position.z);
            var value = point.Position.y;

            Color color;
            if (value > 1)
            {
                color = Color.green;
            }
            else if (value < 0)
            {
                color = Color.red;
            }
            else
            {
                color = Color.white;
            }

            texture.SetPixel(x, y, color);
        }

        texture.Apply(false, false);

        return texture;
    }
}
