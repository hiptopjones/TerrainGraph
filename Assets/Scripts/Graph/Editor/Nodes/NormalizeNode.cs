using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
public class NormalizeNode : Node,
    IValidatableNode,
    IEvaluatableNode<HeightGrid>,
    IPreviewableNode
{
    private class InputValues
    {
        public HeightGrid Grid;

        public int GenerationHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(Grid.GenerationHash);
        }
    }

    private HeightGrid _cachedOutputGrid;
    private int _previewGenerationHash;

    // Options
    private const string NODE_OPTION_PREVIEW_ID = "preview_option";
    private const string NODE_OPTION_PREVIEW_TITLE = "Enable Preview";

    // Inputs
    private const string NODE_INPUT_GRID_ID = "grid_input";
    private const string NODE_INPUT_GRID_TITLE = "Grid";

    private const string NODE_INPUT_PREVIEW_ID = "preview_input";
    private const string NODE_INPUT_PREVIEW_TITLE = "Preview";

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
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID_ID, out temp.Grid);

        if (success)
        {
            temp.GenerationHash = temp.GetHashCode();

            input = temp;
            return true;
        }

        return false;
    }

    public bool TryGetOutputValue(IPort _, out HeightGrid value)
    {
        if (!TryExecuteNode())
        {
            value = null;
            return false;
        }

        value = _cachedOutputGrid;
        return true;
    }

    private bool TryExecuteNode()
    {
        if (!TryGetValidatedInputValues(out var inputValues))
        {
            // Not in valid state
            _cachedOutputGrid = null;
            return false;
        }

        if (_cachedOutputGrid != null && _cachedOutputGrid.GenerationHash == inputValues.GenerationHash)
        {
            // Node is already up-to-date
            return true;
        }

        // Clear the cached values in case there's an early exit below
        _cachedOutputGrid = null;

        try
        {
            var inputGrid = inputValues.Grid;

            var size = inputGrid.Width;

            var maxValue = float.MinValue;
            var minValue = float.MaxValue;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var value = inputGrid[x, y];

                    maxValue = Mathf.Max(maxValue, value);
                    minValue = Mathf.Min(minValue, value);
                }
            }

            var outputGrid = new HeightGrid(size);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var value = inputGrid[x, y];

                    outputGrid[x, y] = Mathf.InverseLerp(minValue, maxValue, value);
                }
            }

            outputGrid.GenerationHash = inputValues.GenerationHash;

            _cachedOutputGrid = outputGrid;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
    }

    public bool TryUpdatePreview()
    {
        // Ensure we're up-to-date. Needed for standalone nodes that have nobody else to poke them
        if (!TryExecuteNode())
        {
            // Node execution failed
            return false;
        }

        GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);
        if (!isPreviewEnabled)
        {
            // Force generation when next enabled
            _previewGenerationHash = 0;

            // Preview is disabled, treat as up-to-date
            return true;
        }

        if (_previewGenerationHash == _cachedOutputGrid.GenerationHash)
        {
            // Preview is already up-to-date
            return true;
        }

        if (PreviewHelpers.TryUpdatePreview(this, NODE_INPUT_PREVIEW_ID, _cachedOutputGrid))
        {
            // Cache generation value to avoid unnecessary updates
            _previewGenerationHash = _cachedOutputGrid.GenerationHash;

            // Preview was successfully updated
            return true;
        }

        // Preview could not be updated
        return false;
    }
}