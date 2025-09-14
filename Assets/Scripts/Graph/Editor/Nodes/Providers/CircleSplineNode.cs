using System;
using Unity.GraphToolkit.Editor;

[Serializable]
public class CircleSplineNode : ProviderNode<SplineProvider>
{
    private class InputValues
    {
        public int Size;

        public int VersionHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(Size);
        }
    }

    // Options

    // Inputs
    private const string NODE_INPUT_SIZE_ID = "size_input";
    private const string NODE_INPUT_SIZE_TITLE = "Size";

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
        context.AddInputPort<int>(NODE_INPUT_SIZE_ID)
            .WithDisplayName(NODE_INPUT_SIZE_TITLE)
            .WithDefaultValue(256)
            .Build();

        // Output
        context.AddOutputPort<SplineProvider>(NODE_OUTPUT_PROVIDER_ID)
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
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SIZE_ID, out temp.Size);

        if (success)
        {
            temp.VersionHash = temp.GetHashCode();

            input = temp;
            return true;
        }

        return false;
    }

    public override bool TryGetOutputValue(IPort _, out SplineProvider value)
    {
        if (!TryGetValidatedInputValues(out var inputValues))
        {
            value = null;
            return false;
        }

        value = new CircleSplineProvider()
        {
            Size = inputValues.Size,
            
            VersionHash = inputValues.VersionHash
        };

        return true;
    }
}