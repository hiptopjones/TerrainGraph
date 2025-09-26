using System;
using UnityEngine;
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
