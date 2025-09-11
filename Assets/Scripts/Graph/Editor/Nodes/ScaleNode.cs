using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
public class ScaleNode : Node,
    IValidatableNode,
    IEvaluatableNode<HeightGrid>,
    IPreviewableNode
{
    private class InputValues
    {
        public HeightGrid Grid;
        public Vector2 ScalePercent;

        public int GenerationHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(Grid.GenerationHash, ScalePercent);
        }
    }

    private HeightGrid _cachedOutputGrid;

    // Options
    private const string NODE_OPTION_PREVIEW_ID = "preview_option";
    private const string NODE_OPTION_PREVIEW_TITLE = "Enable Preview";

    // Inputs
    private const string NODE_INPUT_GRID_ID = "grid_input";
    private const string NODE_INPUT_GRID_TITLE = "Grid";

    private const string NODE_INPUT_SCALE_ID = "scale_input";
    private const string NODE_INPUT_SCALE_TITLE = "Scale";

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
        context.AddInputPort<Vector2>(NODE_INPUT_SCALE_ID)
            .WithDisplayName(NODE_INPUT_SCALE_TITLE)
            .WithDefaultValue(Vector2.one)
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
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID_ID, out temp.Grid) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SCALE_ID, out temp.ScalePercent);

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
            var scalePercent = inputValues.ScalePercent;

            var size = inputGrid.Width;

            var outputGrid = new HeightGrid(size);

            var center = Vector2.one * size / 2;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var target = new Vector2(x, y);
                    var source = (target - center) / scalePercent + center;

                    if (source.x < 0 || source.x > size - 1 ||
                        source.y < 0 || source.y > size - 1)
                    {
                        outputGrid[x, y] = 0;
                    }
                    else
                    {
                        var x1 = Mathf.FloorToInt(source.x);
                        var y1 = Mathf.FloorToInt(source.y);
                        var x2 = Mathf.FloorToInt(source.x + 1);
                        var y2 = Mathf.FloorToInt(source.y + 1);

                        var q11 = GridHelpers.SafeGet(inputGrid, x1, y1);
                        var q21 = GridHelpers.SafeGet(inputGrid, x2, y1);
                        var q22 = GridHelpers.SafeGet(inputGrid, x2, y2);
                        var q12 = GridHelpers.SafeGet(inputGrid, x1, y2);

                        outputGrid[x, y] = GeometryHelpers.BilinearInterpolate(source.x, source.y, q11, q21, q22, q12, x1, y1, x2, y2);
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