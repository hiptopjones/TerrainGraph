using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    public static class TextureHelpers
    {
        private const int PREVIEW_SIZE = 256;

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

        public static bool TryCreatePreviewTexture(IVersionedObject value, out Texture texture)
        {
            texture = null;

            switch (value)
            {
                case HeightGrid grid:
                    return TryCreateHeightGridPreviewTexture(grid, out texture);

                case SplineWrapper splineWrapper:
                    return TryCreateSplineWrapperPreviewTexture(splineWrapper, out texture);

                default:
                    Debug.LogError($"Unhandled data type: {value.GetType().Name}");
                    return false;
            }
        }

        public static bool TryCreateHeightGridPreviewTexture(HeightGrid grid, out Texture texture)
        {
            RenderTexture outputTexture = null;

            try
            {
                var shader = Resources.Load<Shader>("HeightGridColorizer");
                if (shader == null)
                {
                    Debug.LogError($"Unable to find colorizer shader");

                    texture = null;
                    return false;
                }

                outputTexture = CreateRenderTexture(PREVIEW_SIZE, RenderTextureFormat.ARGB32);
                var material = new Material(shader);

                Graphics.Blit(grid.RenderTexture, outputTexture, material);

                texture = outputTexture;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);

                if (outputTexture != null)
                {
                    Object.DestroyImmediate(outputTexture);
                    outputTexture = null;
                }

                texture = null;
                return false;
            }
        }

        public static bool TryCreateSplineWrapperPreviewTexture(SplineWrapper splineWrapper, out Texture texture)
        {
            const int MARGIN_WIDTH = 5;

            Texture2D outputTexture = null;

            try
            {
                var spline = splineWrapper.Spline;
                var length = spline.GetLength();
                var bounds = spline.GetBounds();
                var center = bounds.center;

                var size = SplineHelpers.GetMinimumBoundingSquareSize(spline, MARGIN_WIDTH);
                var halfSize = new float3(size / 2, 0, size / 2);

                if (size <= 0 || size > Mathf.Pow(2, 14))
                {
                    Debug.LogError($"Spline size is invalid: {size} (valid: 0 < n < 16384)");

                    texture = null;
                    return false;
                }

                var width = size;
                var height = size;

                outputTexture = new Texture2D(width, height, TextureFormat.RGB24, mipChain: false, linear: true);
                outputTexture.wrapMode = TextureWrapMode.Clamp;
                outputTexture.filterMode = FilterMode.Bilinear;

                // Clear the texture
                for (int y = 0; y < outputTexture.height; y++)
                {
                    for (int x = 0; x < outputTexture.width; x++)
                    {
                        outputTexture.SetPixel(x, y, Color.black);
                    }
                }

                var firstPosition = Vector3.zero;
                var previousPosition = Vector3.zero;

                // Draw the spline outline
                for (int i = 0; i < length; i++)
                {
                    var t = i / (length - 1);
                    if (spline.Closed)
                    {
                        t = i / length;
                    }

                    var p = spline.EvaluatePosition(t);



                    p = p - (float3)center + halfSize;



                    var currentPosition = new Vector2(p.x, p.z);

                    if (i > 0)
                    {
                        DrawLine(outputTexture, previousPosition, currentPosition, Color.white);
                    }

                    previousPosition = currentPosition;


                    if (i == 0)
                    {
                        firstPosition = currentPosition;
                    }
                }

                if (spline.Closed)
                {
                    DrawLine(outputTexture, previousPosition, firstPosition, Color.white);
                }

                AddExecutionTime(splineWrapper.ExecutionTime, outputTexture);

                outputTexture.Apply(false, false);

                texture = outputTexture;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);

                texture = null;
                return false;
            }
        }

        public static void DrawLine(Texture2D texture, Vector2 start, Vector2 end, Color color)
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
                    texture.SetPixel(x0, y0, color);
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

        public static Texture2D CreateTexture(int width, int height, TextureFormat format)
        {
            var texture = new Texture2D(width, height, format, mipChain: false, linear: true);
            return texture;
        }

        public static RenderTexture CreateRenderTexture(int size, RenderTextureFormat format)
        {
            var renderTexture = new RenderTexture(size, size, 0, format);

            renderTexture.enableRandomWrite = true;
            renderTexture.Create();

            return renderTexture;
        }

        public static bool TryCopyRenderTextureToTexture2D(RenderTexture renderTexture, TextureFormat textureFormat, out Texture2D texture)
        {
            Texture2D outputTexture = null;

            try
            {
                RenderTexture savedRenderTexture = RenderTexture.active;

                RenderTexture.active = renderTexture;

                outputTexture = new Texture2D(renderTexture.width, renderTexture.height, textureFormat, false);

                outputTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                outputTexture.Apply();

                RenderTexture.active = savedRenderTexture;

                texture = outputTexture;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);

                if (outputTexture != null)
                {
                    Object.DestroyImmediate(outputTexture);
                    outputTexture = null;
                }

                texture = null;
                return false;
            }
        }
    }
}
