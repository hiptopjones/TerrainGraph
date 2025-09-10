using System;
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
        public BlendMethod BlendMethod;
        public HeightGrid Grid1;
        public HeightGrid Grid2;

        public int GenerationHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(BlendMethod, Grid1?.GenerationHash, Grid2?.GenerationHash);
        }
    }

    private enum BlendMethod
    {
        Add = 100,
        Subtract = 200,
        Multiply = 300,
        Divide = 400,
        Minimum = 500,
        Maximum = 600,
        Average = 700,
    }

    private HeightGrid _cachedOutputGrid;

    // Options
    private const string NODE_OPTION_METHOD_ID = "method_option";
    private const string NODE_OPTION_METHOD_TITLE = "Blend Method";
    
    private const string NODE_OPTION_PREVIEW_ID = "preview_option";
    private const string NODE_OPTION_PREVIEW_TITLE = "Enable Preview";

    // Input
    private const string NODE_INPUT_GRID1_ID = "grid1_input";
    private const string NODE_INPUT_GRID1_TITLE = "Grid 1";

    private const string NODE_INPUT_GRID2_ID = "grid2_input";
    private const string NODE_INPUT_GRID2_TITLE = "Grid 2";

    private const string NODE_INPUT_PREVIEW_ID = "preview_input";
    private const string NODE_INPUT_PREVIEW_TITLE = "Preview";

    // Output
    private const string NODE_OUTPUT_GRID_ID = "grid_output";
    private const string NODE_OUTPUT_GRID_TITLE = "Grid";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<BlendMethod>(NODE_OPTION_METHOD_ID)
            .WithDisplayName(NODE_OPTION_METHOD_TITLE)
            .WithDefaultValue(BlendMethod.Maximum)
            .Build();
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

        if (!Enum.IsDefined(typeof(BlendMethod), input.BlendMethod))
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_OPTION_METHOD_TITLE} option invalid", this);
            isValid = false;
        }

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

        if (isValid && input.Grid1.Values.Length != input.Grid2.Values.Length)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID1_TITLE} and {NODE_INPUT_GRID2_TITLE} size mismatch", this);
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
            GetNodeOptionByName(NODE_OPTION_METHOD_ID).TryGetValue(out temp.BlendMethod) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID1_ID, out temp.Grid1) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID2_ID, out temp.Grid2);

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
            var blendMethod = inputValues.BlendMethod;
            var inputGrid1 = inputValues.Grid1;
            var inputGrid2 = inputValues.Grid2;

            Func<float, float, float> blendFunction = GetBlendFunction(blendMethod);

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

    private Func<float, float, float> GetBlendFunction(BlendMethod blendMethod)
    {
        switch (blendMethod)
        {
            case BlendMethod.Add:
                return BlendFunctions.Add;

            case BlendMethod.Subtract:
                return BlendFunctions.Subtract;

            case BlendMethod.Multiply:
                return BlendFunctions.Multiply;

            case BlendMethod.Divide:
                return BlendFunctions.Divide;

            case BlendMethod.Minimum:
                return BlendFunctions.Minimum;

            case BlendMethod.Maximum:
                return BlendFunctions.Maximum;

            case BlendMethod.Average:
                return BlendFunctions.Average;

            default:
                Debug.LogError($"Unhandled blend method: {blendMethod}");
                return BlendFunctions.Invalid;
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