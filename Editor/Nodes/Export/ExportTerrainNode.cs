using System;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Windows;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ExportTerrainNode
        : BaseNode<OptionValuesBase, ExportTerrainNode.InputValues, NullOutput>, IExportableNode
    {
        public class InputValues : InputValuesBase
        {
            public HeightGrid Grid;

            [DisplayName("Terrain")]
            [DefaultValue("My Terrain Data")]
            [ValidIf(nameof(IsValidTarget))]
            public string TargetAssetName;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    Grid?.VersionHash, TargetAssetName
                );
            }
        }

        private bool IsValidTarget(InputValues inputs, GraphLogger graphLogger)
        {
            var inputDisplayName = NodeHelpers.GetDisplayName(typeof(InputValues), nameof(InputValues.TargetAssetName));
            var gridDisplayName = NodeHelpers.GetDisplayName(typeof(InputValues), nameof(InputValues.Grid));

            var isValid = true;

            if (string.IsNullOrEmpty(inputs.TargetAssetName))
            {
                graphLogger?.LogError($"{inputDisplayName} value missing", this);
                isValid = false;
            }
            else
            {
                var terrainDataGuids = AssetDatabase.FindAssets($"t:TerrainData {inputs.TargetAssetName}");

                var terrainDataGuidCount = terrainDataGuids.Length;
                if (terrainDataGuidCount == 0)
                {
                    graphLogger?.LogError($"{inputDisplayName} value invalid", this);
                    isValid = false;
                }
                else if (terrainDataGuidCount > 1)
                {
                    graphLogger?.LogError($"{inputDisplayName} value ambiguous", this);
                    isValid = false;
                }
                else
                {
                    var terrainDataGuid = terrainDataGuids.First();
                    var assetFilePath = AssetDatabase.GUIDToAssetPath(terrainDataGuid);

                    var terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(assetFilePath);

                    var terrainSize = terrainData.heightmapResolution - 1;

                    if (inputs.Grid.Size != terrainSize)
                    {
                        graphLogger?.LogError($"{gridDisplayName} and {inputDisplayName} heightmap resolution mismatch", this);
                        isValid = false;
                    }
                }
            }

            return isValid;
        }

        protected override bool TryExecuteNodeInternal()
        {
            return true;
        }

        public bool TryExportNode()
        {
            if (Inputs == null)
            {
                // Node is not in valid state
                return false;
            }

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
