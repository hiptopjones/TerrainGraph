using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
public class ExportTerrainNode : Node,
    IValidatableNode,
    IExecutableNode
{
    private class InputValues
    {
        public HeightGrid Grid;
        public TerrainDataWrapper TerrainDataWrapper;

        public int VersionHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(Grid?.VersionHash, TerrainDataWrapper?.TerrainData);
        }
    }

    // Options

    // Inputs
    private const string NODE_INPUT_GRID_ID = "grid_input";
    private const string NODE_INPUT_GRID_TITLE = "Grid";

    private const string NODE_INPUT_TERRAIN_ID = "terrain_input";
    private const string NODE_INPUT_TERRAIN_TITLE = "Terrain";

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
    }

    public bool TryValidateNode(GraphLogger graphLogger = null)
    {
        return TryGetValidatedInputValues(out _, graphLogger);
    }

    private bool TryGetValidatedInputValues(out InputValues validatedInput, GraphLogger graphLogger = null)
    {
        Action<string, object> LogError = (m, o) =>
        {
            if (graphLogger != null)
            {
                graphLogger.LogError(m, o);
            }
            else
            {
                Debug.LogError(m);
            }
        };

        validatedInput = null;

        if (!TryGetInputValues(out var input))
        {
            LogError("Upstream failure", this);
            return false;
        }

        var isValid = true;

        if (input.Grid == null || !input.Grid.IsValid)
        {
            LogError($"{NODE_INPUT_GRID_TITLE} value missing", this);
            isValid = false;
        }

        if (input.TerrainDataWrapper == null || !input.TerrainDataWrapper.IsValid)
        {
            LogError($"{NODE_INPUT_TERRAIN_TITLE} value missing", this);
            isValid = false;
        }
        else
        {
            var terrainData = input.TerrainDataWrapper.TerrainData;
            var terrainSize = terrainData.heightmapResolution - 1;

            if (input.Grid.Size != terrainSize)
            {
                LogError($"{NODE_INPUT_GRID_TITLE} and {NODE_INPUT_TERRAIN_TITLE} size mismatch", this);
                isValid = false;
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
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID_ID, out temp.Grid);
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_TERRAIN_ID, out temp.TerrainDataWrapper);

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
            Debug.Log("Failed validation");
            return false;
        }

        try
        {
            var inputGrid = inputValues.Grid;
            var inputTerrainWrapper = inputValues.TerrainDataWrapper;

            var terrainData = inputTerrainWrapper.TerrainData;

            var size = inputGrid.Size;

            var heights = new float[size, size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // NOTE: Unity's heightmap is indexed in reverse
                    heights[y, x] = inputGrid[x, y];
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
    }
}
