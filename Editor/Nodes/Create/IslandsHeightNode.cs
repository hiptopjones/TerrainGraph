using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class IslandsHeightNode
        : BaseNode<IslandsHeightNode.OptionValues, IslandsHeightNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            public HeightGrid Grid;
        }

        protected override bool TryExecuteNodeInternal()
        {
            Texture2D workingTexture = null;

            try
            {
                var inputGrid = Inputs.Grid;

                var size = inputGrid.Size;

                var inputTexture = inputGrid.RenderTexture;

                if (!TextureHelpers.TryCopyRenderTextureToTexture2D(inputTexture, TextureFormat.RFloat, out workingTexture))
                {
                    return false;
                }

                var rawHeights = new float[size * size];
                var heights = new float[size, size];

                var rawTextureData = workingTexture.GetRawTextureData<float>();
                rawTextureData.CopyTo(rawHeights);

                GridHelpers.CopyHeights(rawHeights, heights);

                var clusters = GridHelpers.GetClusters(heights).OrderByDescending(c => c.Count).ToList();

                for (int i = 0; i < clusters.Count; i++)
                {
                    var cluster = clusters[i];

                    foreach (var point in cluster)
                    {
                        // Use non-zero color values
                        var color = new Color(i + 1, 0, 0);

                        workingTexture.SetPixel(point.x, point.y, color);
                    }
                }

                workingTexture.Apply();

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                Graphics.Blit(workingTexture, outputTexture);

                var outputGrid = new HeightGrid(size);

                outputGrid.RenderTexture = outputTexture;
                outputGrid.VersionHash = Inputs.VersionHash;

                CacheData.Output = outputGrid;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
            finally
            {
                if (workingTexture != null)
                {
                    Object.DestroyImmediate(workingTexture);
                    workingTexture = null;
                }
            }
        }
    }
}