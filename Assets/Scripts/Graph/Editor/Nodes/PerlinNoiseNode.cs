using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
public class PerlinNoiseNode : Node,
    IValidatableNode,
    IEvaluatableNode<HeightGrid>,
    IPreviewableNode
{
    private int _generationId;
    private HeightGrid _cachedOutput;

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
        var isValid = true;

        PortEvaluator.TryEvaluateInputPort<int>(this, NODE_INPUT_SIZE_ID, _generationId, out var size);
        if (size <= 0)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SIZE_TITLE} value invalid: {size} (valid: 0 < n)", this);
            isValid = false;
        }

        PortEvaluator.TryEvaluateInputPort<int>(this, NODE_INPUT_OCTAVES_ID, _generationId, out var octaves);
        if (octaves <= 0)
        {
            if (graphLogger != null) graphLogger.LogError($"Invalid {NODE_INPUT_OCTAVES_TITLE} specified: {octaves} (valid: 0 < n)", this);
            isValid = false;
        }

        return isValid;
    }

    public void ResetNode(int generationId)
    {
        _generationId = generationId;
        _cachedOutput = null;
    }

    public bool TryGetPortValue(IPort _, int generationId, out HeightGrid value)
    {
        if (!TryExecuteNode(generationId))
        {
            value = null;
            return false;
        }

        value = _cachedOutput;
        return true;
    }

    private bool TryExecuteNode(int generationId)
    {
        if (!TryValidateNode())
        {
            // Node validation did not pass
            return false;
        }

        if (_generationId == generationId)
        {
            // Node is already up-to-date
            return true;
        }

        ResetNode(generationId);

        try
        {
            PortEvaluator.TryEvaluateInputPort<int>(this, NODE_INPUT_SIZE_ID, _generationId, out var size);
            PortEvaluator.TryEvaluateInputPort<Vector2>(this, NODE_INPUT_POSITION_ID, _generationId, out var position);
            PortEvaluator.TryEvaluateInputPort<float>(this, NODE_INPUT_FREQUENCY_ID, _generationId, out var frequency);
            PortEvaluator.TryEvaluateInputPort<float>(this, NODE_INPUT_AMPLITUDE_ID, _generationId, out var amplitude);
            PortEvaluator.TryEvaluateInputPort<int>(this, NODE_INPUT_OCTAVES_ID, _generationId, out var octaves);
            PortEvaluator.TryEvaluateInputPort<float>(this, NODE_INPUT_PERSISTENCE_ID, _generationId, out var persistence);
            PortEvaluator.TryEvaluateInputPort<float>(this, NODE_INPUT_LACUNARITY_ID, _generationId, out var lacunarity);
            PortEvaluator.TryEvaluateInputPort<int>(this, NODE_INPUT_SEED_ID, _generationId, out var seed);
            PortEvaluator.TryEvaluateInputPort<Vector2>(this, NODE_INPUT_RANGE_ID, _generationId, out var range);
            PortEvaluator.TryEvaluateInputPort<float>(this, NODE_INPUT_SCALE_ID, _generationId, out var scale);

            var noise = NoiseHelpers.GeneratePerlinNoise(size, position, frequency, amplitude, octaves, persistence, lacunarity, seed);

            var output = new HeightGrid(size);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var height = noise[x, y];
                    height = range.x + (range.y - range.x) * height;

                    output[x, y] = height * scale;
                }
            }

            _cachedOutput = output;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
    }

    public void UpdatePreview(int generationId)
    {
        GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);
        if (isPreviewEnabled)
        {
            PreviewHelpers.UpdatePreview(this, NODE_INPUT_PREVIEW_ID, NODE_OUTPUT_GRID_ID, generationId);
        }
    }
}
