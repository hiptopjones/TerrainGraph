using System;
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

    public static bool TryCreateHeightMapTexture(HeightGrid grid, out Texture2D texture)
    {
        try
        {
            var width = grid.Width;
            var height = grid.Height;

            texture = new Texture2D(width, height, TextureFormat.R16, mipChain: false, linear: true);
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
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);

            texture = null;
            return false;
        }
    }

    public static bool TryCreatePreviewTexture(IVersionedData value, out Texture2D texture)
    {
        texture = null;

        switch (value)
        {
            case HeightGrid grid:
                return TryCreatePreviewTexture(grid, out texture);

            case SplineWrapper spline:
                return TryCreatePreviewTexture(spline, out texture);

            default:
                Debug.LogError($"Unhandled data type: {value.GetType().Name}");
                return false;
        }
    }

    public static bool TryCreatePreviewTexture(HeightGrid grid, out Texture2D texture)
    {
        try
        {
            var width = grid.Width;
            var height = grid.Height;

            texture = new Texture2D(width, height, TextureFormat.RGB24, mipChain: false, linear: true);
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
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);

            texture = null;
            return false;
        }
    }

    // TODO: Instead, use the spline-to-mask conversion, and then reuse the above method
    public static bool TryCreatePreviewTexture(SplineWrapper spline, out Texture2D texture)
    {
        try
        {
            var width = spline.Size;
            var height = spline.Size;

            texture = new Texture2D(width, height, TextureFormat.RGB24, mipChain: false, linear: true);
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

            // TODO: Can we draw the spline, rather than just the vertices?

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
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);

            texture = null;
            return false;
        }
    }
}
