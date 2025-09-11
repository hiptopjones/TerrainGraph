using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
public class ResizeNode : Node,
    IValidatableNode,
    IEvaluatableNode<HeightGrid>,
    IPreviewableNode
{
    private class InputValues
    {
        public HeightGrid Grid;
        public int Size;

        public int GenerationHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(Grid.GenerationHash, Size);
        }
    }

    private HeightGrid _cachedOutputGrid;

    // Options
    private const string NODE_OPTION_PREVIEW_ID = "preview_option";
    private const string NODE_OPTION_PREVIEW_TITLE = "Enable Preview";

    // Inputs
    private const string NODE_INPUT_GRID_ID = "grid_input";
    private const string NODE_INPUT_GRID_TITLE = "Grid";

    private const string NODE_INPUT_SIZE_ID = "size_input";
    private const string NODE_INPUT_SIZE_TITLE = "Size";

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
        context.AddInputPort<int>(NODE_INPUT_SIZE_ID)
            .WithDisplayName(NODE_INPUT_SIZE_TITLE)
            .WithDefaultValue(256)
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

        if (input.Size <= 0)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SIZE_TITLE} value invalid: {input.Size} (valid: 0 < n)", this);
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
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SIZE_ID, out temp.Size);

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

        try
        {
            var inputGrid = inputValues.Grid;
            var outputSize = inputValues.Size;

            var inputSize = inputGrid.Width;

            var outputGrid = new HeightGrid(outputSize);

            var outputCenter = Vector2Int.one * outputSize / 2;
            var inputCenter = Vector2Int.one * inputSize / 2;

            for (int y = 0; y < outputSize; y++)
            {
                for (int x = 0; x < outputSize; x++)
                {
                    var target = new Vector2Int(x, y);
                    var source = target - outputCenter + inputCenter;

                    if (source.x < 0 || source.x > inputSize - 1 ||
                        source.y < 0 || source.y > inputSize - 1)
                    {
                        outputGrid[target.x, target.y] = 0;
                    }
                    else
                    {
                        outputGrid[target.x, target.y] = inputGrid[source.x, source.y];
                    }
                }
            }

            _cachedOutputGrid = outputGrid;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
    }

    public void UpdatePreview()
    {
        // Ensure we're up-to-date. Needed for standalone nodes that have nobody else to poke them
        TryExecuteNode();

        GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);
        if (isPreviewEnabled)
        {
            PreviewHelpers.TryUpdatePreview(this, NODE_INPUT_PREVIEW_ID, _cachedOutputGrid);
        }
    }
}