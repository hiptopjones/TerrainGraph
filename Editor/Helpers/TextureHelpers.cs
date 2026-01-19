using Indiecat.UnityCommon.Runtime;
using System;
using System.IO;
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
                    texture.SetPixel(x, y, Color.black);
                }
            }
        }

        public static bool TryCreatePreviewTexture(IVersionedObject value, out Texture texture, out int gridSize)
        {
            gridSize = 0;
            texture = null;

            switch (value)
            {
                case HeightGrid grid:
                    return TryCreateHeightGridPreviewTexture(grid, out texture, out gridSize);

                case SplineWrapper splineWrapper:
                    return TryCreateSplineWrapperPreviewTexture(splineWrapper, out texture, out gridSize);

                case SplineListWrapper splineListWrapper:
                    return TryCreateSplineListWrapperPreviewTexture(splineListWrapper, out texture, out gridSize);

                default:
                    Debug.LogError($"Unhandled data type: {value.GetType().Name}");
                    return false;
            }
        }

        public static bool TryCreateHeightGridPreviewTexture(HeightGrid grid, out Texture texture, out int gridSize)
        {
            RenderTexture outputTexture = null;

            try
            {
                var shader = Resources.Load<Shader>("HeightGridColorizer");
                if (shader == null)
                {
                    Debug.LogError($"Unable to find colorizer shader");

                    gridSize = 0;
                    texture = null;
                    return false;
                }

                outputTexture = CreateRenderTexture(PREVIEW_SIZE, RenderTextureFormat.ARGB32);
                var material = new Material(shader);

                Graphics.Blit(grid.RenderTexture, outputTexture, material);

                gridSize = grid.RenderTexture.width;
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

                gridSize = 0;
                texture = null;
                return false;
            }
        }

        public static bool TryCreateSplineWrapperPreviewTexture(SplineWrapper splineWrapper, out Texture texture, out int gridSize)
        {
            const int MARGIN_WIDTH = 5;

            Texture2D outputTexture = null;

            try
            {
                var spline = splineWrapper.Spline;

                var bounds = SplineHelpers.GetMinimumBoundingSquare(spline, MARGIN_WIDTH);
                var width = (int)bounds.size.x;
                var height = width;

                outputTexture = CreateTexture(width, height, TextureFormat.RGB24);
                ClearTexture(outputTexture);

                DrawSpline(outputTexture, spline, bounds);

                AddExecutionTime(splineWrapper.ExecutionTime, outputTexture);

                outputTexture.Apply(false, false);

                gridSize = width;
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

                gridSize = 0;
                texture = null;
                return false;
            }
        }

        private static bool TryCreateSplineListWrapperPreviewTexture(SplineListWrapper splineListWrapper, out Texture texture, out int gridSize)
        {
            const int MARGIN_WIDTH = 5;

            Texture2D outputTexture = null;

            try
            {
                var splines = splineListWrapper.Splines;

                var bounds = SplineHelpers.GetMinimumBoundingSquare(splines, MARGIN_WIDTH);
                var width = (int)bounds.size.x;
                var height = width;

                outputTexture = CreateTexture(width, height, TextureFormat.RGB24);
                ClearTexture(outputTexture);

                foreach (var spline in splines)
                {
                    DrawSpline(outputTexture, spline, bounds);
                }

                AddExecutionTime(splineListWrapper.ExecutionTime, outputTexture);

                outputTexture.Apply(false, false);

                gridSize = width;
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

                gridSize = 0;
                texture = null;
                return false;
            }
        }

        private static void DrawSpline(Texture2D texture, Spline spline, Bounds bounds)
        {
            var length = spline.GetLength();

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

                var p = (Vector3)spline.EvaluatePosition(t);
                p = p - bounds.min;

                var currentPosition = ((Vector3)p).WithY(0);

                if (i > 0)
                {
                    var color = Color.white;
                    if (currentPosition.y > 1)
                    {
                        color = Color.green;
                    }
                    else if (currentPosition.y < 0)
                    {
                        color = Color.red;
                    }
                    else
                    {
                        color = Color.white;
                    }

                    DrawLine(texture, previousPosition.SwizzleXZ(), currentPosition.SwizzleXZ(), color);
                }

                previousPosition = currentPosition;

                if (i == 0)
                {
                    firstPosition = currentPosition;
                }
            }

            if (spline.Closed)
            {
                DrawLine(texture, previousPosition.SwizzleXZ(), firstPosition.SwizzleXZ(), Color.white);
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

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            return texture;
        }

        public static RenderTexture CreateRenderTexture(int size, RenderTextureFormat format)
        {
            var renderTexture = new RenderTexture(size, size, 0, format);

            renderTexture.enableRandomWrite = true;
            renderTexture.wrapMode = TextureWrapMode.Clamp;
            renderTexture.filterMode = FilterMode.Bilinear;
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

                outputTexture = CreateTexture(renderTexture.width, renderTexture.height, textureFormat);
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

        public static Texture2D GetRampTexture(int width, Func<float, float> rampFunction)
        {
            var height = 1;

            var colors = new Color[width];

            for (int x = 0; x < width; x++)
            {
                var t = x / (float)(width - 1);
                colors[x] = new Color(rampFunction(t), 0, 0, 0);
            }

            var texture = CreateTexture(width, height, TextureFormat.RFloat);

            texture.SetPixels(colors);
            texture.Apply();

            return texture;
        }

        public static bool TryExportHeightGridTexture(HeightGrid inputGrid, string exportFilePath)
        {
            Texture2D exportTexture = null;

            try
            {
                var renderTexture = inputGrid.RenderTexture;

                if (!TryCopyRenderTextureToTexture2D(renderTexture, TextureFormat.R16, out exportTexture))
                {
                    return false;
                }

                var bytes = exportTexture.EncodeToPNG();

                Directory.CreateDirectory(Path.GetDirectoryName(exportFilePath));

                exportFilePath = Path.ChangeExtension(exportFilePath, "png");
                File.WriteAllBytes(exportFilePath, bytes);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
            finally
            {
                if (exportTexture != null)
                {
                    Object.DestroyImmediate(exportTexture);
                    exportTexture = null;
                }
            }
        }
    }
}
