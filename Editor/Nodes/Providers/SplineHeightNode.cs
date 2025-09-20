using System;
using Unity.GraphToolkit.Editor;

[Serializable]
public class SplineHeightNode : ProviderNode<IProvider>
{
    private class InputValues
    {
        public SplineWrapper SplineWrapper;
        public int Samples;
        public bool Center;
        public bool IncludeEdgeHeight;

        public int VersionHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(SplineWrapper?.VersionHash, Samples, Center, IncludeEdgeHeight);
        }
    }

    // Options

    // Inputs
    private const string NODE_INPUT_SPLINE_ID = "spline_input";
    private const string NODE_INPUT_SPLINE_TITLE = "Spline";

    private const string NODE_INPUT_SAMPLES_ID = "samples_input";
    private const string NODE_INPUT_SAMPLES_TITLE = "Samples";

    private const string NODE_INPUT_CENTER_ID = "center_input";
    private const string NODE_INPUT_CENTER_TITLE = "Center";

    private const string NODE_INPUT_EDGE_ID = "edge_input";
    private const string NODE_INPUT_EDGE_TITLE = "Include Edge Height";

    // Outputs
    private const string NODE_OUTPUT_PROVIDER_ID = "provider_output";
    private const string NODE_OUTPUT_PROVIDER_TITLE = "Provider";

    private const int MIN_SAMPLES = 10;

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        // N/A
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        // Input
        context.AddInputPort<SplineWrapper>(NODE_INPUT_SPLINE_ID)
            .WithDisplayName(NODE_INPUT_SPLINE_TITLE)
            .Build();
        context.AddInputPort<int>(NODE_INPUT_SAMPLES_ID)
            .WithDisplayName(NODE_INPUT_SAMPLES_TITLE)
            .WithDefaultValue(100)
            .Build();
        context.AddInputPort<bool>(NODE_INPUT_CENTER_ID)
            .WithDisplayName(NODE_INPUT_CENTER_TITLE)
            .WithDefaultValue(true)
            .Build();
        context.AddInputPort<bool>(NODE_INPUT_EDGE_ID)
            .WithDisplayName(NODE_INPUT_EDGE_TITLE)
            .WithDefaultValue(false)
            .Build();

        // Output
        context.AddOutputPort<IProvider>(NODE_OUTPUT_PROVIDER_ID)
            .WithDisplayName(NODE_OUTPUT_PROVIDER_TITLE)
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

        if (input.SplineWrapper == null || !input.SplineWrapper.IsValid)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SPLINE_TITLE} value missing", this);
            isValid = false;
        }

        if (input.Samples < MIN_SAMPLES)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SAMPLES_TITLE} value invalid: {input.Samples} (valid: n < {MIN_SAMPLES})", this);
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
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SPLINE_ID, out temp.SplineWrapper) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SAMPLES_ID, out temp.Samples) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_CENTER_ID, out temp.Center) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_EDGE_ID, out temp.IncludeEdgeHeight);

        if (success)
        {
            temp.VersionHash = temp.GetHashCode();

            input = temp;
            return true;
        }

        return false;
    }

    public override bool TryGetOutputValue(IPort _, out IProvider value)
    {
        if (!TryGetValidatedInputValues(out var inputValues))
        {
            value = null;
            return false;
        }

        value = new SplineHeightProvider()
        {
            Spline = inputValues.SplineWrapper.Spline,
            Samples = inputValues.Samples,
            Center = inputValues.Center,
            IncludeEdgeHeight = inputValues.IncludeEdgeHeight,

            VersionHash = inputValues.VersionHash
        };

        return true;
    }
}