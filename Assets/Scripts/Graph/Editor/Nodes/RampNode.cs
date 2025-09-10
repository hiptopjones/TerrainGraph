using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
public class RampNode : Node,
    IValidatableNode,
    IEvaluatableNode<HeightGrid>,
    IPreviewableNode
{
    private class InputValues
    {
        public HeightGrid Grid;
        public RampType RampType;
        public AnimationCurve Curve;
        public Gradient Gradient;

        public int GenerationHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(Grid.GenerationHash, Curve);
        }
    }

    private enum RampType
    {
        Curve = 100,
        Gradient = 200
    }

    private HeightGrid _cachedOutputGrid;

    // Options
    private const string NODE_OPTION_TYPE_ID = "type_option";
    private const string NODE_OPTION_TYPE_TITLE = "Ramp Type";

    private const string NODE_OPTION_PREVIEW_ID = "preview_option";
    private const string NODE_OPTION_PREVIEW_TITLE = "Enable Preview";

    // Inputs
    private const string NODE_INPUT_GRID_ID = "grid_input";
    private const string NODE_INPUT_GRID_TITLE = "Grid";

    private const string NODE_INPUT_CURVE_ID = "curve_input";
    private const string NODE_INPUT_CURVE_TITLE = "Curve";

    private const string NODE_INPUT_GRADIENT_ID = "gradient_input";
    private const string NODE_INPUT_GRADIENT_TITLE = "Gradient";

    private const string NODE_INPUT_PREVIEW_ID = "preview_input";
    private const string NODE_INPUT_PREVIEW_TITLE = "Preview";

    // Outputs
    private const string NODE_OUTPUT_GRID_ID = "grid_output";
    private const string NODE_OUTPUT_GRID_TITLE = "Height Grid";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<RampType>(NODE_OPTION_TYPE_ID)
            .WithDisplayName(NODE_OPTION_TYPE_TITLE)
            .WithDefaultValue(RampType.Curve)
            .Build();
        context.AddOption<bool>(NODE_OPTION_PREVIEW_ID)
            .WithDisplayName(NODE_OPTION_PREVIEW_TITLE)
            .WithDefaultValue(false)
            .Build();
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);
        GetNodeOptionByName(NODE_OPTION_TYPE_ID).TryGetValue<RampType>(out var rampType);

        // Input
        context.AddInputPort<HeightGrid>(NODE_INPUT_GRID_ID)
            .WithDisplayName(NODE_INPUT_GRID_TITLE)
            .Build();

        switch (rampType)
        {
            case RampType.Gradient:
                context.AddInputPort<Gradient>(NODE_INPUT_GRADIENT_ID)
                    .WithDisplayName(NODE_INPUT_GRADIENT_TITLE)
                    .WithDefaultValue(GetDefaultGradient())
                    .Build();
                break;

            case RampType.Curve:
            default:
                context.AddInputPort<AnimationCurve>(NODE_INPUT_CURVE_ID)
                    .WithDisplayName(NODE_INPUT_CURVE_TITLE)
                    .WithDefaultValue(AnimationCurve.EaseInOut(0, 0, 1, 1))
                    .Build();
                break;
        }

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

    private Gradient GetDefaultGradient()
    {
        var gradient = new Gradient();

        gradient.SetColorKeys(new[]
        {
            new GradientColorKey(Color.black, 0),
            new GradientColorKey(Color.gray, 0.5f),
            new GradientColorKey(Color.white, 1)
        });

        return gradient;
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
            GetNodeOptionByName(NODE_OPTION_TYPE_ID).TryGetValue<RampType>(out temp.RampType) &&
            (temp.RampType != RampType.Curve || PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_CURVE_ID, out temp.Curve)) &&
            (temp.RampType != RampType.Gradient || PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRADIENT_ID, out temp.Gradient));

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

            var remapFunction = GetRemapFunction(inputValues);

            var size = inputGrid.Width;

            var outputGrid = new HeightGrid(size);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    outputGrid[x, y] = remapFunction(inputGrid[x, y]);
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

    private Func<float, float> GetRemapFunction(InputValues inputValues)
    {
        var curve = inputValues.Curve;
        var gradient = inputValues.Gradient;

        switch (inputValues.RampType)
        {
            case RampType.Curve:
                return (t) => curve.Evaluate(t);

            case RampType.Gradient:
                return (t) => gradient.Evaluate(t).grayscale;

            default:
                Debug.LogError($"Unhandled remap type: {inputValues.RampType}");
                return (t) => 1;
        }
    }

    public void UpdatePreview()
    {
        // Ensure we're up-to-date. Needed for standalone nodes that have nobody else to poke them
        TryExecuteNode();

        GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);
        if (isPreviewEnabled)
        {
            PreviewHelpers.UpdatePreview(this, NODE_INPUT_PREVIEW_ID, _cachedOutputGrid);
        }
    }
}