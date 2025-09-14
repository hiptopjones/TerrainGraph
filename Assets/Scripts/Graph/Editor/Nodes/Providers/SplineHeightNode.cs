using System;
using Unity.GraphToolkit.Editor;

[Serializable]
public class SplineHeightNode : ProviderNode<HeightProvider>
{
    private class InputValues
    {
        public SplineWrapper Spline;
        public int Size;

        public int VersionHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(Spline.VersionHash, Size);
        }
    }

    // Options

    // Inputs
    private const string NODE_INPUT_SPLINE_ID = "spline_input";
    private const string NODE_INPUT_SPLINE_TITLE = "Spline";

    // Outputs
    private const string NODE_OUTPUT_PROVIDER_ID = "provider_output";
    private const string NODE_OUTPUT_PROVIDER_TITLE = "Provider";

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

        // Output
        context.AddOutputPort<HeightProvider>(NODE_OUTPUT_PROVIDER_ID)
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

        if (input.Spline == null || !input.Spline.IsValid)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SPLINE_TITLE} value missing", this);
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
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SPLINE_ID, out temp.Spline);

        if (success)
        {
            temp.VersionHash = temp.GetHashCode();

            input = temp;
            return true;
        }

        return false;
    }

    public override bool TryGetOutputValue(IPort _, out HeightProvider value)
    {
        if (!TryGetValidatedInputValues(out var inputValues))
        {
            value = null;
            return false;
        }

        value = new SplineHeightProvider()
        {
            Spline = inputValues.Spline.Spline,

            VersionHash = inputValues.VersionHash
        };

        return true;
    }
}