using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class PerlinNoiseNode : Node, IValidatedNode, IEvaluatedNode<float[,]>
{
    private float[,] _grid;

    // Options
    internal const string OPTION_ENABLE_PREVIEW_ID = "enable_preview";

    // Inputs
    internal const string INPUT_PORT_SIZE_ID = "size";
    internal const string INPUT_PORT_POSITION_ID = "position";
    internal const string INPUT_PORT_FREQUENCY_ID = "frequency";
    internal const string INPUT_PORT_AMPLITUDE_ID = "amplitude";
    internal const string INPUT_PORT_OCTAVES_ID = "octaves";
    internal const string INPUT_PORT_PERSISTENCE_ID = "persistence";
    internal const string INPUT_PORT_LACUNARITY_ID = "lacunarity";
    internal const string INPUT_PORT_SEED_ID = "seed";
    internal const string INPUT_PORT_RANGE_ID = "range";
    internal const string INPUT_PORT_SCALE_ID = "scale";
    internal const string INPUT_PORT_PREVIEW_ID = "preview";

    // Outputs
    internal const string OUTPUT_PORT_GRID_ID = "grid";

    public override void OnEnable()
    {
        ResetNode();
    }

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<bool>(OPTION_ENABLE_PREVIEW_ID)
            .WithDisplayName("Enable Preview")
            .WithDefaultValue(false)
            .Build();
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        GetNodeOptionByName(OPTION_ENABLE_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);

        // Input
        context.AddInputPort<int>(INPUT_PORT_SIZE_ID)
            .WithDisplayName("Size")
            .WithDefaultValue(256)
            .Build();
        context.AddInputPort<Vector2>(INPUT_PORT_POSITION_ID)
            .WithDisplayName("Position")
            .Build();
        context.AddInputPort<float>(INPUT_PORT_FREQUENCY_ID)
            .WithDisplayName("Frequency")
            .WithDefaultValue(0.05f)
            .Build();
        context.AddInputPort<float>(INPUT_PORT_AMPLITUDE_ID)
            .WithDisplayName("Amplitude")
            .WithDefaultValue(1)
            .Build();
        context.AddInputPort<int>(INPUT_PORT_OCTAVES_ID)
            .WithDisplayName("Octaves")
            .WithDefaultValue(3)
            .Build();
        context.AddInputPort<float>(INPUT_PORT_PERSISTENCE_ID)
            .WithDisplayName("Persistence")
            .WithDefaultValue(0.5f)
            .Build();
        context.AddInputPort<float>(INPUT_PORT_LACUNARITY_ID)
            .WithDisplayName("Lacunarity")
            .WithDefaultValue(2)
            .Build();
        context.AddInputPort<int>(INPUT_PORT_SEED_ID)
            .WithDisplayName("Seed")
            .Build();
        context.AddInputPort<Vector2>(INPUT_PORT_RANGE_ID)
            .WithDisplayName("Range")
            .WithDefaultValue(new Vector2(0, 1))
            .Build();
        context.AddInputPort<float>(INPUT_PORT_SCALE_ID)
            .WithDisplayName("Scale")
            .WithDefaultValue(0.1f)
            .Build();

        if (isPreviewEnabled)
        {
            context.AddInputPort<PreviewImage>(INPUT_PORT_PREVIEW_ID)
                .WithDisplayName("Preview")
                .Build();
        }

        // Output
        context.AddOutputPort<float[,]>(OUTPUT_PORT_GRID_ID)
            .WithDisplayName("Grid")
            .Build();
    }

    public void ValidateNode(GraphLogger graphLogger)
    {
        // TODO
    }

    public void ResetNode()
    {
        _grid = null;
    }

    public bool TryGetPortValue(IPort _, out float[,] value)
    {
        if (_grid == null)
        {
            // Only execute on demand
            if (!TryExecuteNode())
            {
                value = null;
                return false;
            }
        }

        value = _grid;
        return true;
    }

    private bool TryExecuteNode()
    {
        try
        {
            var size = PortEvaluator.EvaluatePort<int>(GetInputPortByName(INPUT_PORT_SIZE_ID));
            if (size <= 0)
            {
                return false;
            }

            var position = PortEvaluator.EvaluatePort<Vector2>(GetInputPortByName(INPUT_PORT_POSITION_ID));
            var frequency = PortEvaluator.EvaluatePort<float>(GetInputPortByName(INPUT_PORT_FREQUENCY_ID));
            var amplitude = PortEvaluator.EvaluatePort<float>(GetInputPortByName(INPUT_PORT_AMPLITUDE_ID));
            var octaves = PortEvaluator.EvaluatePort<int>(GetInputPortByName(INPUT_PORT_OCTAVES_ID));
            if (octaves <= 0)
            {
                return false;
            }

            var persistence = PortEvaluator.EvaluatePort<float>(GetInputPortByName(INPUT_PORT_PERSISTENCE_ID));
            var lacunarity = PortEvaluator.EvaluatePort<float>(GetInputPortByName(INPUT_PORT_LACUNARITY_ID));
            var seed = PortEvaluator.EvaluatePort<int>(GetInputPortByName(INPUT_PORT_SEED_ID));
            var range = PortEvaluator.EvaluatePort<Vector2>(GetInputPortByName(INPUT_PORT_RANGE_ID));
            var scale = PortEvaluator.EvaluatePort<float>(GetInputPortByName(INPUT_PORT_SCALE_ID));

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

            _grid = noise;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
    }
}