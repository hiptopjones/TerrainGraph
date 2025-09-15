using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
public class RangeNode : ExecutableNode<HeightGrid>
{
    private class InputValues
    {
        public HeightGrid Grid;
        public Vector2 FromRange;
        public Vector2 ToRange;

        public int VersionHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(Grid.VersionHash, FromRange, ToRange);
        }
    }

    // Options

    // Inputs
    private const string NODE_INPUT_GRID_ID = "grid_input";
    private const string NODE_INPUT_GRID_TITLE = "Grid";

    private const string NODE_INPUT_FROM_ID = "from_input";
    private const string NODE_INPUT_FROM_TITLE = "From Range";

    private const string NODE_INPUT_TO_ID = "to_input";
    private const string NODE_INPUT_TO_TITLE = "To Range";

    // Outputs
    private const string NODE_OUTPUT_GRID_ID = "grid_output";
    private const string NODE_OUTPUT_GRID_TITLE = "Grid";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<bool>(NODE_OPTION_PREVIEW_ID)
            .WithDisplayName(NODE_OPTION_PREVIEW_TITLE)
            .WithDefaultValue(false)
            .Build();
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);

        // Input
        context.AddInputPort<HeightGrid>(NODE_INPUT_GRID_ID)
            .WithDisplayName(NODE_INPUT_GRID_TITLE)
            .Build();
        context.AddInputPort<Vector2>(NODE_INPUT_FROM_ID)
            .WithDisplayName(NODE_INPUT_FROM_TITLE)
            .WithDefaultValue(new Vector2(0, 1))
            .Build();
        context.AddInputPort<Vector2>(NODE_INPUT_TO_ID)
            .WithDisplayName(NODE_INPUT_TO_TITLE)
            .WithDefaultValue(new Vector2(0, 1))
            .Build();

        if (isPreviewEnabled)
        {
            context.AddInputPort<PreviewImage>(NODE_INPUT_PREVIEW_ID)
                .WithDisplayName(NODE_INPUT_PREVIEW_TITLE)
                .Build();
        }

        // Output
        context.AddOutputPort<HeightGrid>(NODE_OUTPUT_GRID_ID)
            .WithDisplayName(NODE_OUTPUT_GRID_TITLE)
            .Build();
    }

    public override bool TryValidateNode(GraphLogger graphLogger = null)
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

        if (input.FromRange.x == input.FromRange.y)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_FROM_TITLE} value invalid (x != y)", this);
            isValid = false;
        }

        if (input.ToRange.x == input.ToRange.y)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_TO_TITLE} value invalid (x != y)", this);
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
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_FROM_ID, out temp.FromRange) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_TO_ID, out temp.ToRange);

        if (success)
        {
            temp.VersionHash = temp.GetHashCode();

            input = temp;
            return true;
        }

        return false;
    }

    public override bool TryGetOutputValue(IPort _, out HeightGrid value)
    {
        if (!TryExecuteNode())
        {
            value = null;
            return false;
        }

        value = CacheData.Output;
        return true;
    }

    public override bool TryExecuteNode()
    {
        if (!TryGetValidatedInputValues(out var inputValues))
        {
            // Not in valid state
            CacheData.Output = null;
            return false;
        }

        if (CacheData.Output != null && CacheData.Output.VersionHash == inputValues.VersionHash)
        {
            // Node is already up-to-date
            return true;
        }

        // Clear the cached values in case there's an early exit below
        CacheData.Output = null;

        try
        {
            var inputGrid = inputValues.Grid;
            var fromRange = inputValues.FromRange;
            var toRange = inputValues.ToRange;

            var size = inputGrid.Size;

            var outputGrid = new HeightGrid(size);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var fromValue = inputGrid[x, y];
                    var t = Mathf.InverseLerp(fromRange.x, fromRange.y, fromValue);
                    var toValue = Mathf.Lerp(toRange.x, toRange.y, t);
                    outputGrid[x, y] = toValue;
                }
            }

            outputGrid.VersionHash = inputValues.VersionHash;

            CacheData.Output = outputGrid;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
    }
}