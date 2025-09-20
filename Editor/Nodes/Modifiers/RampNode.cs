using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
public class RampNode : ExecutableNode<HeightGrid>
{
    private class InputValues
    {
        public RampType RampType;
        public HeightGrid Grid;
        public AnimationCurve Curve;
        public Gradient Gradient;

        public int VersionHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(RampType, Grid?.VersionHash, Curve, GetHashCode(Gradient));
        }

        private int GetHashCode(Gradient gradient)
        {
            // Gradient's hashcode doesn't seem to respond to value changes, and
            // the colorKeys property's hashcode changes each time its retrieved.
            // So we build our own hashcode to detect the changes we care about.
            var hashCode = new HashCode();

            foreach (var key in gradient.colorKeys)
            {
                hashCode.Add(key.color.grayscale);
                hashCode.Add(key.time);
            }

            return hashCode.ToHashCode();
        }
    }

    private enum RampType
    {
        Curve = 100,
        Gradient = 200
    }

    // Options
    private const string NODE_OPTION_TYPE_ID = "type_option";
    private const string NODE_OPTION_TYPE_TITLE = "Ramp Type";

    // Inputs
    private const string NODE_INPUT_GRID_ID = "grid_input";
    private const string NODE_INPUT_GRID_TITLE = "Grid";

    private const string NODE_INPUT_CURVE_ID = "curve_input";
    private const string NODE_INPUT_CURVE_TITLE = "Curve";

    private const string NODE_INPUT_GRADIENT_ID = "gradient_input";
    private const string NODE_INPUT_GRADIENT_TITLE = "Gradient";

    // Outputs
    private const string NODE_OUTPUT_GRID_ID = "grid_output";
    private const string NODE_OUTPUT_GRID_TITLE = "Grid";

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

        if (!Enum.IsDefined(typeof(RampType), input.RampType))
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_OPTION_TYPE_TITLE} option invalid", this);
            isValid = false;
        }

        if (input.Grid == null || !input.Grid.IsValid)
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
            GetNodeOptionByName(NODE_OPTION_TYPE_ID).TryGetValue<RampType>(out temp.RampType) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID_ID, out temp.Grid) &&
            (temp.RampType != RampType.Curve || PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_CURVE_ID, out temp.Curve)) &&
            (temp.RampType != RampType.Gradient || PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRADIENT_ID, out temp.Gradient));

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

        var startTime = DateTime.Now;
        if (TryExecuteNodeInternal(inputValues))
        {
            CacheData.Output.ExecutionTime = (float)(DateTime.Now - startTime).TotalSeconds;
            return true;
        }

        return false;
    }

    private bool TryExecuteNodeInternal(InputValues inputValues)
    {
        try
        {
            var inputGrid = inputValues.Grid;

            var rampFunction = GetRampFunction(inputValues);

            var size = inputGrid.Size;

            var outputGrid = new HeightGrid(size);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    outputGrid[x, y] = rampFunction(Mathf.Clamp01(inputGrid[x, y]));
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

    private Func<float, float> GetRampFunction(InputValues inputValues)
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
}