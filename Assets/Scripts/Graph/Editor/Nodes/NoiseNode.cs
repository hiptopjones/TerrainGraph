using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
public class NoiseNode : Node,
    IValidatableNode,
    IEvaluatableNode<HeightGrid>,
    IPreviewableNode
{
    private class InputValues
    {
        public int Size;
        public Vector2 Position;
        public float Frequency;
        public float Amplitude;
        public int Octaves;
        public float Persistence;
        public float Lacunarity;
        public int Seed;
        public Vector2 Range;
        public float Scale;

        public int GenerationHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(
                HashCode.Combine(Size, Position, Frequency, Amplitude, Octaves),
                HashCode.Combine(Persistence, Lacunarity, Seed, Range, Scale)
            );
        }
    }

    private HeightGrid _cachedOutputGrid;

    // Options
    private const string NODE_OPTION_PREVIEW_ID = "preview_option";
    private const string NODE_OPTION_PREVIEW_TITLE = "Enable Preview";

    // Inputs
    private const string NODE_INPUT_SIZE_ID = "size_input";
    private const string NODE_INPUT_SIZE_TITLE = "Size";

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

    private const string NODE_INPUT_RANGE_ID = "range_input";
    private const string NODE_INPUT_RANGE_TITLE = "Range";

    private const string NODE_INPUT_SCALE_ID = "scale_input";
    private const string NODE_INPUT_SCALE_TITLE = "Scale";

    private const string NODE_INPUT_PREVIEW_ID = "preview_input";
    private const string NODE_INPUT_PREVIEW_TITLE = "Preview";

    // Outputs
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
        context.AddInputPort<int>(NODE_INPUT_SIZE_ID)
            .WithDisplayName(NODE_INPUT_SIZE_TITLE)
            .WithDefaultValue(256)
            .Build();
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
        context.AddInputPort<Vector2>(NODE_INPUT_RANGE_ID)
            .WithDisplayName(NODE_INPUT_RANGE_TITLE)
            .WithDefaultValue(new Vector2(0, 1))
            .Build();
        context.AddInputPort<float>(NODE_INPUT_SCALE_ID)
            .WithDisplayName(NODE_INPUT_SCALE_TITLE)
            .WithDefaultValue(0.1f)
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

        if (input.Size <= 0)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SIZE_TITLE} value invalid: {input.Size} (valid: 0 < n)", this);
            isValid = false;
        }

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
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SIZE_ID, out temp.Size) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_POSITION_ID, out temp.Position) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_FREQUENCY_ID, out temp.Frequency) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_AMPLITUDE_ID, out temp.Amplitude) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_OCTAVES_ID, out temp.Octaves) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_PERSISTENCE_ID, out temp.Persistence) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_LACUNARITY_ID, out temp.Lacunarity) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SEED_ID, out temp.Seed) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_RANGE_ID, out temp.Range) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SCALE_ID, out temp.Scale);

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
            var size = inputValues.Size;
            var position = inputValues.Position;
            var frequency = inputValues.Frequency;
            var amplitude = inputValues.Amplitude;
            var octaves = inputValues.Octaves;
            var persistence = inputValues.Persistence;
            var lacunarity = inputValues.Lacunarity;
            var seed = inputValues.Seed;
            var range = inputValues.Range;
            var scale = inputValues.Scale;

            var noise = NoiseHelpers.GeneratePerlinNoise(size, position, frequency, amplitude, octaves, persistence, lacunarity, seed);

            var outputGrid = new HeightGrid(size);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var height = noise[x, y];
                    height = range.x + (range.y - range.x) * height;

                    outputGrid[x, y] = height * scale;
                }
            }

            outputGrid.GenerationHash = inputValues.GenerationHash;

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
            PreviewHelpers.UpdatePreview(this, NODE_INPUT_PREVIEW_ID, _cachedOutputGrid);
        }
    }
}
