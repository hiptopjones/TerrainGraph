using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
public class PerlinNoiseNode : Node, IValidatedNode, IEvaluatedNode<float[,]>
{
    private float[,] _cachedOutput;

    // Options
    internal const string NODE_OPTION_ENABLE_PREVIEW_ID = TerrainEditorGraph.NODE_OPTION_PREVIEW_ID;

    // Inputs
    internal const string NODE_INPUT_SIZE_ID = "size_input";
    internal const string NODE_INPUT_POSITION_ID = "position_input";
    internal const string NODE_INPUT_FREQUENCY_ID = "frequency_input";
    internal const string NODE_INPUT_AMPLITUDE_ID = "amplitude_input";
    internal const string NODE_INPUT_OCTAVES_ID = "octaves_input";
    internal const string NODE_INPUT_PERSISTENCE_ID = "persistence_input";
    internal const string NODE_INPUT_LACUNARITY_ID = "lacunarity_input";
    internal const string NODE_INPUT_SEED_ID = "seed_input";
    internal const string NODE_INPUT_RANGE_ID = "range_input";
    internal const string NODE_INPUT_SCALE_ID = "scale_input";
    internal const string NODE_INPUT_PREVIEW_ID = TerrainEditorGraph.NODE_INPUT_PREVIEW_ID;

    // Outputs
    internal const string NODE_OUTPUT_GRID_ID = TerrainEditorGraph.NODE_OUTPUT_GRID_ID;

    public override void OnEnable()
    {
        ResetNode();
    }

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<bool>(NODE_OPTION_ENABLE_PREVIEW_ID)
            .WithDisplayName("Enable Preview")
            .WithDefaultValue(false)
            .Build();
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        GetNodeOptionByName(NODE_OPTION_ENABLE_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);

        // Input
        context.AddInputPort<int>(NODE_INPUT_SIZE_ID)
            .WithDisplayName("Size")
            .WithDefaultValue(256)
            .Build();
        context.AddInputPort<Vector2>(NODE_INPUT_POSITION_ID)
            .WithDisplayName("Position")
            .Build();
        context.AddInputPort<float>(NODE_INPUT_FREQUENCY_ID)
            .WithDisplayName("Frequency")
            .WithDefaultValue(0.05f)
            .Build();
        context.AddInputPort<float>(NODE_INPUT_AMPLITUDE_ID)
            .WithDisplayName("Amplitude")
            .WithDefaultValue(1)
            .Build();
        context.AddInputPort<int>(NODE_INPUT_OCTAVES_ID)
            .WithDisplayName("Octaves")
            .WithDefaultValue(3)
            .Build();
        context.AddInputPort<float>(NODE_INPUT_PERSISTENCE_ID)
            .WithDisplayName("Persistence")
            .WithDefaultValue(0.5f)
            .Build();
        context.AddInputPort<float>(NODE_INPUT_LACUNARITY_ID)
            .WithDisplayName("Lacunarity")
            .WithDefaultValue(2)
            .Build();
        context.AddInputPort<int>(NODE_INPUT_SEED_ID)
            .WithDisplayName("Seed")
            .Build();
        context.AddInputPort<Vector2>(NODE_INPUT_RANGE_ID)
            .WithDisplayName("Range")
            .WithDefaultValue(new Vector2(0, 1))
            .Build();
        context.AddInputPort<float>(NODE_INPUT_SCALE_ID)
            .WithDisplayName("Scale")
            .WithDefaultValue(0.1f)
            .Build();

        if (isPreviewEnabled)
        {
            context.AddInputPort<PreviewImage>(NODE_INPUT_PREVIEW_ID)
                .WithDisplayName("Preview")
                .Build();
        }

        // Output
        context.AddOutputPort<float[,]>(NODE_OUTPUT_GRID_ID)
            .WithDisplayName("Grid")
            .Build();
    }

    public void ValidateNode(GraphLogger graphLogger)
    {
        // TODO
    }

    public void ResetNode()
    {
        _cachedOutput = null;
    }

    public bool TryGetPortValue(IPort _, out float[,] value)
    {
        if (_cachedOutput == null)
        {
            // Only execute on demand
            if (!TryExecuteNode())
            {
                value = null;
                return false;
            }
        }

        value = _cachedOutput;
        return true;
    }

    private bool TryExecuteNode()
    {
        try
        {
            var size = PortEvaluator.EvaluatePort<int>(GetInputPortByName(NODE_INPUT_SIZE_ID));
            if (size <= 0)
            {
                return false;
            }

            var position = PortEvaluator.EvaluatePort<Vector2>(GetInputPortByName(NODE_INPUT_POSITION_ID));
            var frequency = PortEvaluator.EvaluatePort<float>(GetInputPortByName(NODE_INPUT_FREQUENCY_ID));
            var amplitude = PortEvaluator.EvaluatePort<float>(GetInputPortByName(NODE_INPUT_AMPLITUDE_ID));
            var octaves = PortEvaluator.EvaluatePort<int>(GetInputPortByName(NODE_INPUT_OCTAVES_ID));
            if (octaves <= 0)
            {
                return false;
            }

            var persistence = PortEvaluator.EvaluatePort<float>(GetInputPortByName(NODE_INPUT_PERSISTENCE_ID));
            var lacunarity = PortEvaluator.EvaluatePort<float>(GetInputPortByName(NODE_INPUT_LACUNARITY_ID));
            var seed = PortEvaluator.EvaluatePort<int>(GetInputPortByName(NODE_INPUT_SEED_ID));
            var range = PortEvaluator.EvaluatePort<Vector2>(GetInputPortByName(NODE_INPUT_RANGE_ID));
            var scale = PortEvaluator.EvaluatePort<float>(GetInputPortByName(NODE_INPUT_SCALE_ID));

            var noise = NoiseHelpers.GeneratePerlinNoise(size, position, frequency, amplitude, octaves, persistence, lacunarity, seed);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var height = noise[x, y];
                    height = range.x + (range.y - range.x) * height;

                    noise[x, y] = height * scale;
                }
            }

            _cachedOutput = noise;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
    }
}