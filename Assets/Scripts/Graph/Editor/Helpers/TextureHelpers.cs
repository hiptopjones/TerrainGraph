using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

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

    public static bool TryCreatePreviewTexture(SplineWrapper splineWrapper, out Texture2D texture)
    {
        const float VERTEX_INTERVAL_LENGTH = 20;

        try
        {
            var width = splineWrapper.Size;
            var height = splineWrapper.Size;

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

            var spline = splineWrapper.Spline;
            var length = spline.GetLength();

            var vertexCount = length / VERTEX_INTERVAL_LENGTH;

            var firstPosition = Vector3.zero;
            var previousPosition = Vector3.zero;

            // Draw the spline outline
            for (int i = 0; i < vertexCount; i++)
            {
                var t = i / vertexCount;

                var p = spline.EvaluatePosition(t);

                var currentPosition = new Vector2(p.x, p.z);

                if (i > 0)
                {
                    DrawLine(texture, previousPosition, currentPosition);
                }

                previousPosition = currentPosition;

                if (i == 0)
                {
                    firstPosition = currentPosition;
                }
            }

            if (spline.Closed)
            {
                DrawLine(texture, previousPosition, firstPosition);
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

    public static void DrawLine(Texture2D texture, Vector2 start, Vector2 end)
    {
        int x0 = Mathf.RoundToInt(start.x);
        int y0 = Mathf.RoundToInt(start.y);
        int x1 = Mathf.RoundToInt(end.x);
        int y1 = Mathf.RoundToInt(end.y);

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            if (x0 >= 0 && x0 < texture.width &&
                y0 >= 0 && y0 < texture.height)
            {
                texture.SetPixel(x0, y0, Color.white);
            }

            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }
}
