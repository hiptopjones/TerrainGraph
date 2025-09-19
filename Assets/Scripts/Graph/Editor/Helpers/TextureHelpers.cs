using System;
using Unity.Mathematics;
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
            var width = grid.Size;
            var height = grid.Size;

            texture = new Texture2D(width, height, TextureFormat.R16, mipChain: false, linear: true);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
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

    public static bool TryCreatePreviewTexture(IVersionedObject value, out Texture2D texture)
    {
        texture = null;

        switch (value)
        {
            case HeightGrid grid:
                return TryCreatePreviewTexture(grid, out texture);

            case SplineWrapper splineWrapper:
                return TryCreatePreviewTexture(splineWrapper, out texture);

            default:
                Debug.LogError($"Unhandled data type: {value.GetType().Name}");
                return false;
        }
    }

    public static bool TryCreatePreviewTexture(HeightGrid grid, out Texture2D texture)
    {
        try
        {
            var width = grid.Size;
            var height = grid.Size;

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
                        color = new Color(0, value / texture.height + 0.1f, 0);
                    }
                    else if (value < 0)
                    {
                        color = new Color(Mathf.Abs(value / texture.height) + 0.1f, 0, 0);
                    }
                    else
                    {
                        color = new Color(value, value, value);
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            AddExecutionTime(grid.ExecutionTime, texture);

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
        // Bigger numbers result in nicer previews, but cost performance
        const float SEGMENT_LENGTH_FACTOR = 100;

        texture = null;

        try
        {
            var spline = splineWrapper.Spline;
            var length = spline.GetLength();
            var bounds = spline.GetBounds();
            var center = bounds.center;

            var size = Mathf.CeilToInt(Mathf.Max(bounds.size.x, bounds.size.z));

            if (size <= 0 || size > Mathf.Pow(2, 14))
            {
                Debug.LogError($"Spline size is invalid: {size} (valid: 0 < n < 16384)");
                return false;
            }

            var width = size;
            var height = size;

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

            // This is a preview texture, and is used to get a sense of shape
            // Scale the vertex count based on the bounds of the spline
            // This ensures big splines and small splines are represented reasonably well
            var vertexCount = length / (size / SEGMENT_LENGTH_FACTOR);

            var firstPosition = Vector3.zero;
            var previousPosition = Vector3.zero;

            // Draw the spline outline
            for (int i = 0; i < vertexCount; i++)
            {
                var t = i / (vertexCount - 1);

                var p = spline.EvaluatePosition(t);

                p = p - (float3)center + new float3(size / 2, 0, size / 2);

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

            AddExecutionTime(splineWrapper.ExecutionTime, texture);

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

    private static void AddExecutionTime(float executionTime, Texture2D texture)
    {
        const int BAR_HEIGHT = 5;
        const float MAX_EXECUTION_TIME = 1f;
        const float MIN_EXECUTION_TIME = 0.01f;

        // Logarithmic normalization
        var executionTimePercent =
            (Mathf.Log10(executionTime) - Mathf.Log10(MIN_EXECUTION_TIME)) /
            (Mathf.Log10(MAX_EXECUTION_TIME) - Mathf.Log10(MIN_EXECUTION_TIME));

        executionTimePercent = Mathf.Clamp01(executionTimePercent);

        for (int y = 0; y < BAR_HEIGHT; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                var t = x / (float)(texture.width - 1);
                if (t < executionTimePercent)
                {
                    texture.SetPixel(x, y, new Color(1, 1, 0));
                }
            }
        }
    }
}
