using System;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;
using static Indiecat.TerrainGraph.Editor.NodeConstants;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ExportTerrainNode : Node,
        IValidatableNode,
        IExecutableNode
    {
        private class InputValues
        {
            public HeightGrid Grid;
            public string TargetAssetName;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(Grid?.VersionHash, TargetAssetName);
            }
        }

        // Options

        // Inputs
        private const string NODE_INPUT_GRID_ID = "grid_input";
        private const string NODE_INPUT_GRID_TITLE = "Grid";

        private const string NODE_INPUT_NAME_ID = "terrain_input";
        private const string NODE_INPUT_NAME_TITLE = "Terrain";

        // Outputs

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<bool>(NODE_OPTION_DISABLE_ID)
                .WithDisplayName(NODE_OPTION_DISABLE_TITLE)
                .WithDefaultValue(false)
                .Build();
            context.AddOption<WarningBanner>(NODE_OPTION_WARNING_ID)
                .WithDisplayName(NODE_OPTION_WARNING_TITLE)
                .Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input
            context.AddInputPort<HeightGrid>(NODE_INPUT_GRID_ID)
                .WithDisplayName(NODE_INPUT_GRID_TITLE)
                .Build();
            context.AddInputPort<AdaptiveLengthStringParameter>(NODE_INPUT_NAME_ID)
                .WithDisplayName(NODE_INPUT_NAME_TITLE)
                .WithDefaultValue("My Terrain Data")
                .Build();
        }

        public bool TryValidateNode(GraphLogger graphLogger = null)
        {
            GetNodeOptionByName(NODE_OPTION_DISABLE_ID).TryGetValue(out bool isNodeSkipped);
            this.TrySetWarningBanner(isNodeSkipped ? "DISABLED" : null);
            if (isNodeSkipped)
            {
                return true;
            }

            return TryGetValidatedInputValues(out _, graphLogger);
        }

        private bool TryGetValidatedInputValues(out InputValues validatedInput, GraphLogger graphLogger = null)
        {
            validatedInput = null;

            if (!TryGetInputValues(out var input))
            {
                if (graphLogger != null) graphLogger.LogError("Upstream failure", this);
                return false;
            }

            var isValid = true;

            if (input.Grid == null || !input.Grid.IsValid)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID_TITLE} value missing", this);
                isValid = false;
            }

            if (string.IsNullOrEmpty(input.TargetAssetName))
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_NAME_TITLE} value missing", this);
                isValid = false;
            }
            else
            {
                var terrainDataGuids = AssetDatabase.FindAssets($"t:TerrainData {input.TargetAssetName}");

                var terrainDataGuidCount = terrainDataGuids.Length;
                if (terrainDataGuidCount == 0)
                {
                    if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_NAME_TITLE} value invalid", this);
                    isValid = false;
                }
                else if (terrainDataGuidCount > 1)
                {
                    if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_NAME_TITLE} value ambiguous", this);
                    isValid = false;
                }
                else
                {
                    var terrainDataGuid = terrainDataGuids.First();
                    var assetFilePath = AssetDatabase.GUIDToAssetPath(terrainDataGuid);

                    var terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(assetFilePath);

                    var terrainSize = terrainData.heightmapResolution - 1;

                    if (input.Grid.Size != terrainSize)
                    {
                        if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID_TITLE} and {NODE_INPUT_NAME_TITLE} heightmap resolution mismatch", this);
                        isValid = false;
                    }
                }
            }

            if (isValid)
            {
                validatedInput = input;
            }

            return isValid;
        }

        private bool TryGetInputValues(out InputValues input)
        {
            input = null;

            var temp = new InputValues();
            var success =
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID_ID, out temp.Grid) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_NAME_ID, out temp.TargetAssetName);

            if (success)
            {
                temp.VersionHash = temp.GetHashCode();

                input = temp;
                return true;
            }

            return false;
        }

        public bool TryExecuteNode()
        {
            GetNodeOptionByName(NODE_OPTION_DISABLE_ID).TryGetValue(out bool isNodeDisabled);
            if (isNodeDisabled)
            {
                // Execution skipped
                return true;
            }

            if (!TryGetValidatedInputValues(out var inputValues))
            {
                // Not in valid state
                return false;
            }

            Texture2D workingTexture = null;

            try
            {
                var inputGrid = inputValues.Grid;
                var inputTargetName = inputValues.TargetAssetName;

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
