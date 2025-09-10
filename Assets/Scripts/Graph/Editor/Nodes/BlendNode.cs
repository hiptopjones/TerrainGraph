using System;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
public class BlendNode : Node,
    IValidatableNode,
    IEvaluatableNode<HeightGrid>,
    IPreviewableNode
{
    private class InputValues
    {
        public HeightGrid Grid1;
        public HeightGrid Grid2;
        public BlendMethod Method;

        public int GenerationHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(Grid1?.GenerationHash, Grid2?.GenerationHash, Method);
        }
    }

    private HeightGrid _cachedOutputGrid;

    // Options
    private const string NODE_OPTION_PREVIEW_ID = "preview_option";
    private const string NODE_OPTION_PREVIEW_TITLE = "Enable Preview";

    // Input
    private const string NODE_INPUT_GRID1_ID = "grid1_input";
    private const string NODE_INPUT_GRID1_TITLE = "Height Grid 1";

    private const string NODE_INPUT_GRID2_ID = "grid2_input";
    private const string NODE_INPUT_GRID2_TITLE = "Height Grid 2";

    private const string NODE_INPUT_METHOD_ID = "method_input";
    private const string NODE_INPUT_METHOD_TITLE = "Method";

    private const string NODE_INPUT_PREVIEW_ID = "preview_input";
    private const string NODE_INPUT_PREVIEW_TITLE = "Preview";

    // Output
    private const string NODE_OUTPUT_GRID_ID = "grid_output";
    private const string NODE_OUTPUT_GRID_TITLE = "Height Grid";

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
        context.AddInputPort<HeightGrid>(NODE_INPUT_GRID1_ID)
            .WithDisplayName(NODE_INPUT_GRID1_TITLE)
            .Build();
        context.AddInputPort<HeightGrid>(NODE_INPUT_GRID2_ID)
            .WithDisplayName(NODE_INPUT_GRID2_TITLE)
            .Build();
        context.AddInputPort<BlendMethod>(NODE_INPUT_METHOD_ID)
            .WithDisplayName(NODE_INPUT_METHOD_TITLE)
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

        if (input.Grid1 == null || input.Grid1.Values == null || input.Grid1.Values.Length == 0)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID1_TITLE} value missing", this);
            isValid = false;
        }

        if (input.Grid2 == null || input.Grid2.Values == null || input.Grid2.Values.Length == 0)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID2_TITLE} value missing", this);
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
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID1_ID, out temp.Grid1) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID2_ID, out temp.Grid2) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_METHOD_ID, out temp.Method);

        if (success)
        {
            temp.GenerationHash = temp.GetHashCode();

            input = temp;
            return true;
        }

        return false;
    }
    public bool TryGetOutputValue(IPort _, out HeightGrid grid)
    {
        if (!TryExecuteNode())
        {
            grid = null;
            return false;
        }

        grid = _cachedOutputGrid;
        return true;
    }

    private bool TryExecuteNode()
    {
        if (!TryGetValidatedInputValues(out var inputValues))
        {
            // Not in valid state
            return false;
        }

        if (_cachedOutputGrid != null && _cachedOutputGrid.GenerationHash == inputValues.GenerationHash)
        {
            // Node is already up-to-date
            return true;
        }

        try
        {
            var inputGrid1 = inputValues.Grid1;
            var inputGrid2 = inputValues.Grid2;
            var method = inputValues.Method;

            Func<float, float, float> blendFunction = null;

            switch (method)
            {
                case BlendMethod.Add:
                    blendFunction = (a, b) => a + b;
                    break;

                case BlendMethod.Multiply:
                    blendFunction = (a, b) => a * b;
                    break;

                default:
                    Debug.Log($"Unhandled blend method: {method}");
                    break;
            }

            // TODO: Validate all lengths are expected
            var size = inputGrid1.Width;

            var outputGrid = new HeightGrid(size);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    outputGrid[x, y] = blendFunction(inputGrid1[x, y], inputGrid2[x, y]);
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
        GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);
        if (isPreviewEnabled)
        {
            PreviewHelpers.UpdatePreview(this, NODE_INPUT_PREVIEW_ID, _cachedOutputGrid);
        }
    }
}