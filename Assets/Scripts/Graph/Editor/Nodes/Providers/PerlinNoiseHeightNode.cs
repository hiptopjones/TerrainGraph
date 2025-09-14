using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
public class PerlinNoiseHeightNode : ProviderNode<HeightProvider>
{
    private class InputValues
    {
        public Vector2 Offset;
        public float Frequency;
        public float Amplitude;
        public int Octaves;
        public float Persistence;
        public float Lacunarity;
        public int Seed;

        public int VersionHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(
                HashCode.Combine(Offset, Frequency, Amplitude, Octaves),
                HashCode.Combine(Persistence, Lacunarity, Seed)
            );
        }
    }

    // Options

    // Inputs
    private const string NODE_INPUT_POSITION_ID = "position_input";
    private const string NODE_INPUT_POSITION_TITLE = "Position";

    private const string NODE_INPUT_FREQUENCY_ID = "frequency_input";
    private const string NODE_INPUT_FREQUENCY_TITLE = "Frequency";

    private const string NODE_INPUT_AMPLITUDE_ID = "amplitude_input";
    private const string NODE_INPUT_AMPLITUDE_TITLE = "Amplitude";

    private const string NODE_INPUT_OCTAVES_ID = "octaves_input";
    private const string NODE_INPUT_OCTAVES_TITLE = "Octaves";

    private const string NODE_INPUT_PERSISTENCE_ID = "persistence_input";
    private const string NODE_INPUT_PERSISTENCE_TITLE = "Persistence";

    private const string NODE_INPUT_LACUNARITY_ID = "lacunarity_input";
    private const string NODE_INPUT_LACUNARITY_TITLE = "Lacunarity";

    private const string NODE_INPUT_SEED_ID = "seed_input";
    private const string NODE_INPUT_SEED_TITLE = "Seed";

    // Outputs
    private const string NODE_OUTPUT_NOISE_ID = "provider_output";
    private const string NODE_OUTPUT_NOISE_TITLE = "Provider";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        // N/A
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        // Input
        context.AddInputPort<Vector2>(NODE_INPUT_POSITION_ID)
            .WithDisplayName(NODE_INPUT_POSITION_TITLE)
            .Build();
        context.AddInputPort<float>(NODE_INPUT_FREQUENCY_ID)
            .WithDisplayName(NODE_INPUT_FREQUENCY_TITLE)
            .WithDefaultValue(0.05f)
            .Build();
        context.AddInputPort<float>(NODE_INPUT_AMPLITUDE_ID)
            .WithDisplayName(NODE_INPUT_AMPLITUDE_TITLE)
            .WithDefaultValue(1)
            .Build();
        context.AddInputPort<int>(NODE_INPUT_OCTAVES_ID)
            .WithDisplayName(NODE_INPUT_OCTAVES_TITLE)
            .WithDefaultValue(3)
            .Build();
        context.AddInputPort<float>(NODE_INPUT_PERSISTENCE_ID)
            .WithDisplayName(NODE_INPUT_PERSISTENCE_TITLE)
            .WithDefaultValue(0.5f)
            .Build();
        context.AddInputPort<float>(NODE_INPUT_LACUNARITY_ID)
            .WithDisplayName(NODE_INPUT_LACUNARITY_TITLE)
            .WithDefaultValue(2)
            .Build();
        context.AddInputPort<int>(NODE_INPUT_SEED_ID)
            .WithDisplayName(NODE_INPUT_SEED_TITLE)
            .Build();

        // Output
        context.AddOutputPort<HeightProvider>(NODE_OUTPUT_NOISE_ID)
            .WithDisplayName(NODE_OUTPUT_NOISE_TITLE)
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

        if (input.Octaves <= 0)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_OCTAVES_TITLE} value invalid: {input.Octaves} (valid: 0 < n)", this);
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
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_POSITION_ID, out temp.Offset) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_FREQUENCY_ID, out temp.Frequency) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_AMPLITUDE_ID, out temp.Amplitude) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_OCTAVES_ID, out temp.Octaves) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_PERSISTENCE_ID, out temp.Persistence) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_LACUNARITY_ID, out temp.Lacunarity) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SEED_ID, out temp.Seed);

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

        // TODO: Should this be cached?
        value = new PerlinNoiseHeightProvider()
        {
            Offset = inputValues.Offset,
            Frequency = inputValues.Frequency,
            Amplitude = inputValues.Amplitude,
            Octaves = inputValues.Octaves,
            Persistence = inputValues.Persistence,
            Lacunarity = inputValues.Lacunarity,
            Seed = inputValues.Seed,

            VersionHash = inputValues.VersionHash
        };

        return true;
    }
}
