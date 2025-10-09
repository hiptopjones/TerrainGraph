using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;
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
            public TerrainDataWrapper TerrainDataWrapper;
            public int TerrainHeight;
            public int TerrainSize;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(Grid?.VersionHash, TerrainDataWrapper?.TerrainData, TerrainHeight, TerrainSize);
            }
        }

        // Options

        // Inputs
        private const string NODE_INPUT_GRID_ID = "grid_input";
        private const string NODE_INPUT_GRID_TITLE = "Grid";

        private const string NODE_INPUT_TERRAIN_ID = "terrain_input";
        private const string NODE_INPUT_TERRAIN_TITLE = "Terrain";

        private const string NODE_INPUT_HEIGHT_ID = "height_input";
        private const string NODE_INPUT_HEIGHT_TITLE = "Height";

        private const string NODE_INPUT_SIZE_ID = "size_input";
        private const string NODE_INPUT_SIZE_TITLE = "Size";

        // Outputs

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input
            context.AddInputPort<HeightGrid>(NODE_INPUT_GRID_ID)
                .WithDisplayName(NODE_INPUT_GRID_TITLE)
                .Build();
            context.AddInputPort<TerrainDataWrapper>(NODE_INPUT_TERRAIN_ID)
                .WithDisplayName(NODE_INPUT_TERRAIN_TITLE)
                .Build();
            context.AddInputPort<int>(NODE_INPUT_HEIGHT_ID)
                .WithDisplayName(NODE_INPUT_HEIGHT_TITLE)
                .Build();
            context.AddInputPort<int>(NODE_INPUT_SIZE_ID)
                .WithDisplayName(NODE_INPUT_SIZE_TITLE)
                .Build();
        }

        public bool TryValidateNode(GraphLogger graphLogger = null)
        {
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

            if (input.TerrainDataWrapper == null || !input.TerrainDataWrapper.IsValid)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_TERRAIN_TITLE} value missing", this);
                isValid = false;
            }
            else
            {
                var terrainData = input.TerrainDataWrapper.TerrainData;
                var terrainSize = terrainData.heightmapResolution - 1;

                if (input.Grid.Size != terrainSize)
                {
                    if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID_TITLE} and {NODE_INPUT_TERRAIN_TITLE} size mismatch", this);
                    isValid = false;
                }
            }

            if (input.TerrainHeight <= 0)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_HEIGHT_TITLE} value invalid: {input.TerrainHeight} (valid: 0 < n)", this);
                isValid = false;
            }

            if (input.TerrainSize <= 0)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SIZE_TITLE} value invalid: {input.TerrainSize} (valid: 0 < n)", this);
                isValid = false;
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
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_TERRAIN_ID, out temp.TerrainDataWrapper) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_HEIGHT_ID, out temp.TerrainHeight) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SIZE_ID, out temp.TerrainSize);

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
            if (!TryGetValidatedInputValues(out var inputValues))
            {
                // Not in valid state
                return false;
            }

            Texture2D workingTexture = null;

            try
            {
                var inputGrid = inputValues.Grid;
                var inputTerrainWrapper = inputValues.TerrainDataWrapper;
                var inputTerrainHeight = inputValues.TerrainHeight;
                var inputTerrainSize = inputValues.TerrainSize;

                var terrainData = inputTerrainWrapper.TerrainData;

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
                terrainData.size = new Vector3(inputTerrainSize, inputTerrainHeight, inputTerrainSize);

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
