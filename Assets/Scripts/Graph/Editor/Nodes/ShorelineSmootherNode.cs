using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

public class ShorelineSmootherNode : Node, IValidatedNode, IEvaluatedNode<float[,]>
{
    private float[,] _cachedOutput;

    // Catch noisy edges around sea level
    private const float SEA_EPSILON = 0.0005f;

    // Options
    internal const string NODE_OPTION_PREVIEW_ID = TerrainEditorGraph.NODE_OPTION_PREVIEW_ID;

    // Inputs
    internal const string NODE_INPUT_GRID_ID = "grid_input";
    internal const string NODE_INPUT_SEA_LEVEL_ID = "sea_level_input";
    internal const string NODE_INPUT_FALLOFF_WIDTH_ID = "falloff_width_input";
    internal const string NODE_INPUT_FALLOFF_CURVE_ID = "falloff_curve_input";
    internal const string NODE_INPUT_PREVIEW_ID = TerrainEditorGraph.NODE_INPUT_PREVIEW_ID;

    // Outputs
    internal const string NODE_OUTPUT_GRID_ID = TerrainEditorGraph.NODE_OUTPUT_GRID_ID;

    public override void OnEnable()
    {
        ResetNode();
    }

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<bool>(NODE_OPTION_PREVIEW_ID)
            .WithDisplayName("Enable Preview")
            .WithDefaultValue(false)
            .Build();
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);

        // Input
        context.AddInputPort<float[,]>(NODE_INPUT_GRID_ID)
            .WithDisplayName("Grid")
            .Build();
        context.AddInputPort<float>(NODE_INPUT_SEA_LEVEL_ID)
            .WithDisplayName("Sea Level")
            .Build();
        context.AddInputPort<float>(NODE_INPUT_FALLOFF_WIDTH_ID)
            .WithDisplayName("Falloff Width")
            .WithDefaultValue(50f)
            .Build();
        context.AddInputPort<AnimationCurve>(NODE_INPUT_FALLOFF_CURVE_ID)
            .WithDisplayName("Falloff Curve")
            .WithDefaultValue(AnimationCurve.EaseInOut(0, 0, 1, 1))
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
            var input = PortEvaluator.EvaluatePort<float[,]>(GetInputPortByName(NODE_INPUT_GRID_ID));
            if (input == null)
            {
                return false;
            }

            var seaLevel = PortEvaluator.EvaluatePort<float>(GetInputPortByName(NODE_INPUT_SEA_LEVEL_ID));
            var falloffWidth = PortEvaluator.EvaluatePort<float>(GetInputPortByName(NODE_INPUT_FALLOFF_WIDTH_ID));
            var falloffCurve = PortEvaluator.EvaluatePort<AnimationCurve>(GetInputPortByName(NODE_INPUT_FALLOFF_CURVE_ID));

            int size = input.GetLength(0);

            var output = new float[size, size];

            // Build binary sea mask: 0 = sea, 1 = land
            // (We’ll compute distances from sea pixels.)
            bool[,] isSea = new bool[size, size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    isSea[y, x] = (input[y, x] <= seaLevel + SEA_EPSILON);
                }
            }

            // Distance transform (2-pass chamfer: 8-neighbour, costs 1/√2 and 1)
            // Distances in pixels:
            float big = 1e9f;
            float[,] distances = new float[size, size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    distances[y, x] = isSea[y, x] ? 0f : big;
                }
            }

            // Pass 1: top-left -> bottom-right
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = distances[y, x];
                    // left
                    if (x > 0) d = Mathf.Min(d, distances[y, x - 1] + 1f);
                    // up
                    if (y > 0) d = Mathf.Min(d, distances[y - 1, x] + 1f);
                    // up-left
                    if (x > 0 && y > 0) d = Mathf.Min(d, distances[y - 1, x - 1] + 1.41421356f);
                    // up-right
                    if (x < size - 1 && y > 0) d = Mathf.Min(d, distances[y - 1, x + 1] + 1.41421356f);
                    distances[y, x] = d;
                }
            }

            // Pass 2: bottom-right -> top-left
            for (int y = size - 1; y >= 0; y--)
            {
                for (int x = size - 1; x >= 0; x--)
                {
                    float d = distances[y, x];
                    // right
                    if (x < size - 1) d = Mathf.Min(d, distances[y, x + 1] + 1f);
                    // down
                    if (y < size - 1) d = Mathf.Min(d, distances[y + 1, x] + 1f);
                    // down-right
                    if (x < size - 1 && y < size - 1) d = Mathf.Min(d, distances[y + 1, x + 1] + 1.41421356f);
                    // down-left
                    if (x > 0 && y < size - 1) d = Mathf.Min(d, distances[y + 1, x - 1] + 1.41421356f);
                    distances[y, x] = d;
                }
            }

            // Blend: within falloffWidth, ramp from sea (0) to original height
            int edits = 0;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = distances[y, x];
                    float height = input[y, x];

                    if (distance <= falloffWidth + 0.0001f)
                    {
                        // 0 at shore -> 1 at outer edge of band
                        float t = Mathf.Clamp01(distance / falloffWidth);
                        t = Mathf.Clamp01(falloffCurve.Evaluate(t));

                        float blendedHeight = Mathf.Lerp(seaLevel, height, t);

                        // Ensure we don't push below sea (guard against noise)
                        if (blendedHeight < seaLevel) blendedHeight = seaLevel;

                        output[y, x] = blendedHeight;
                        edits++;
                    }
                    else
                    {
                        // Outside the falloff band: leave as original
                        output[y, x] = height;
                    }
                }
            }

            Debug.Log($"Shoreline smoothing complete. Edited samples: {edits}. Falloff ~{falloffWidth}.");
            _cachedOutput = output;

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
    }
}