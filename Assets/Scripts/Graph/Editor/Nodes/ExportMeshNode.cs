using System;
using System.IO;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

[Serializable]
public class ExportMeshNode : Node,
    IValidatableNode,
    IExecutableNode
{
    private class InputValues
    {
        public HeightGrid Grid;
        public float HeightScale;
        public string ExportPath;

        public int GenerationHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(Grid.GenerationHash, HeightScale, ExportPath);
        }
    }

    // Options
    //  n/a

    // Inputs
    private const string NODE_INPUT_GRID_ID = "grid_input";
    private const string NODE_INPUT_GRID_TITLE = "Grid";

    private const string NODE_INPUT_SCALE_ID = "scale_input";
    private const string NODE_INPUT_SCALE_TITLE = "Height Scale";

    private const string NODE_INPUT_PATH_ID = "path_input";
    private const string NODE_INPUT_PATH_TITLE = "Path";

    // Outputs
    //  n/a

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        // Input
        context.AddInputPort<HeightGrid>(NODE_INPUT_GRID_ID)
            .WithDisplayName(NODE_INPUT_GRID_TITLE)
            .Build();
        context.AddInputPort<float>(NODE_INPUT_SCALE_ID)
            .WithDisplayName(NODE_INPUT_SCALE_TITLE)
            .WithDefaultValue(100)
            .Build();
        context.AddInputPort<string>(NODE_INPUT_PATH_ID)
            .WithDisplayName(NODE_INPUT_PATH_TITLE)
            .WithDefaultValue("Assets/Models/ExportedMesh.obj")
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

        if (input.Grid == null || input.Grid.Values == null || input.Grid.Values.Length == 0)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID_TITLE} value missing", this);
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
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SCALE_ID, out temp.HeightScale) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_PATH_ID, out temp.ExportPath);

        if (success)
        {
            temp.GenerationHash = temp.GetHashCode();

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

        try
        {
            var inputGrid = inputValues.Grid;
            var heightScale = inputValues.HeightScale;
            var exportPath = inputValues.ExportPath;

            MeshHelpers.ExportMesh(inputGrid, heightScale, exportPath);

            // Ensure the editor picks up any changes
            // NOTE: Unable to invoke a refresh directly during graph asset import
            EditorApplication.delayCall = () => AssetDatabase.Refresh();

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
    }
}
