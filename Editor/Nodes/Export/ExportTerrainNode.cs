using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ExportTerrainNode
        : ExecutableNode<OptionValuesBase, ExportTerrainNode.InputValues, NullOutput>
    {
        public class InputValues : InputValuesBase
        {
            public HeightGrid Grid;

            [DisplayName("Terrain")]
            [DefaultValue("My Terrain Data")]
            public string TargetAssetName;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    Grid?.VersionHash, TargetAssetName
                );
            }
        }

        protected override bool TryExecuteNodeInternal()
        {
            Texture2D workingTexture = null;

            try
            {
                var inputGrid = Inputs.Grid;
                var inputTargetName = Inputs.TargetAssetName;

                var terrainDataGuid = AssetDatabase.FindAssets($"t:TerrainData {inputTargetName}").First();
                var assetFilePath = AssetDatabase.GUIDToAssetPath(terrainDataGuid);
                var terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(assetFilePath);

                var size = inputGrid.Size;

                var renderTexture = inputGrid.RenderTexture;

                if (!TextureHelpers.TryCopyRenderTextureToTexture2D(renderTexture, TextureFormat.RFloat, out workingTexture))
                {
                    return false;
                }

                var rawHeights = new float[size * size];
                var heights = new float[size, size];

                var rawTextureData = workingTexture.GetRawTextureData<float>();
                rawTextureData.CopyTo(rawHeights);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        // NOTE: Unity's heightmap is indexed in reverse
                        heights[y, x] = rawHeights[x + y * size];
                    }
                }

                terrainData.SetHeights(0, 0, heights);

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
