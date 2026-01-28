using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ExportTerrainNode
        : BaseNode<ExportTerrainNode.OptionValues, ExportTerrainNode.InputValues, NullOutput>, IExportableNode
    {
        public class OptionValues : OptionValuesBase
        {
            public override int GetHashCode()
            {
                // Avoid using the base hash code
                return 0;
            }
        }

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
                    Grid?.VersionHash, TargetAssetName
                );
            }
        }

        private ValidationResult IsValidTarget(InputValues inputs)
        {
            var classModel = ClassModelCache.GetClassModel<InputValues>();
            var targetModel = classModel.GetFieldModel(nameof(InputValues.TargetAssetName));
            var gridModel = classModel.GetFieldModel(nameof(InputValues.Grid));

            if (string.IsNullOrEmpty(inputs.TargetAssetName))
            {
                return ValidationResult.Error($"{targetModel.DisplayName} input missing");
            }
            else
            {
                var terrainDataGuids = AssetDatabase.FindAssets($"t:TerrainData {inputs.TargetAssetName}");

                var terrainDataGuidCount = terrainDataGuids.Length;
                if (terrainDataGuidCount == 0)
                {
                    return ValidationResult.Error($"{targetModel.DisplayName} input invalid");
                }
                else if (terrainDataGuidCount > 1)
                {
                    return ValidationResult.Error($"{targetModel.DisplayName} input ambiguous");
                }
                else
                {
                    var terrainDataGuid = terrainDataGuids.First();
                    var assetFilePath = AssetDatabase.GUIDToAssetPath(terrainDataGuid);

                    var terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(assetFilePath);

                    var terrainSize = terrainData.heightmapResolution - 1;

                    if (inputs.Grid.Size != terrainSize)
                    {
                        return ValidationResult.Error(
                            $"{gridModel.DisplayName} and {targetModel.DisplayName} heightmap resolution mismatch");
                    }
                }
            }

            return ValidationResult.Ok();
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
