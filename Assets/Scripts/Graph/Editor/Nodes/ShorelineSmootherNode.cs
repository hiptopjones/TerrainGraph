using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
public class ShorelineSmootherNode : Node,
    IValidatableNode,
    IEvaluatableNode<float[,]>,
    IPreviewableNode
{
    private int _generationId;
    private float[,] _cachedOutput;

    // Catch noisy edges around sea level
    private const float SEA_EPSILON = 0.0005f;

    // Options
    private const string NODE_OPTION_PREVIEW_ID = "preview_option";
    private const string NODE_OPTION_PREVIEW_TITLE = "Enable Preview";

    // Inputs
    private const string NODE_INPUT_GRID_ID = "grid_input";
    private const string NODE_INPUT_GRID_TITLE = "Grid";
    private const string NODE_INPUT_SEA_LEVEL_ID = "sea_level_input";
    private const string NODE_INPUT_SEA_LEVEL_TITLE = "Sea Level";
    private const string NODE_INPUT_FALLOFF_WIDTH_ID = "falloff_width_input";
    private const string NODE_INPUT_FALLOFF_WIDTH_TITLE = "Falloff Width";
    private const string NODE_INPUT_FALLOFF_CURVE_ID = "falloff_curve_input";
    private const string NODE_INPUT_FALLOFF_CURVE_TITLE = "Falloff Curve";

    private const string NODE_INPUT_PREVIEW_ID = "preview_input";
    private const string NODE_INPUT_PREVIEW_TITLE = "Preview";

    // Outputs
    private const string NODE_OUTPUT_GRID_ID = "grid_output";
    private const string NODE_OUTPUT_GRID_TITLE = "Grid";

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
        context.AddInputPort<float[,]>(NODE_INPUT_GRID_ID)
            .WithDisplayName(NODE_INPUT_GRID_TITLE)
            .Build();
        context.AddInputPort<float>(NODE_INPUT_SEA_LEVEL_ID)
            .WithDisplayName(NODE_INPUT_SEA_LEVEL_TITLE)
            .Build();
        context.AddInputPort<float>(NODE_INPUT_FALLOFF_WIDTH_ID)
            .WithDisplayName(NODE_INPUT_FALLOFF_WIDTH_TITLE)
            .WithDefaultValue(50f)
            .Build();
        context.AddInputPort<AnimationCurve>(NODE_INPUT_FALLOFF_CURVE_ID)
            .WithDisplayName(NODE_INPUT_FALLOFF_CURVE_TITLE)
            .WithDefaultValue(AnimationCurve.EaseInOut(0, 0, 1, 1))
            .Build();

        if (isPreviewEnabled)
        {
            context.AddInputPort<PreviewImage>(NODE_INPUT_PREVIEW_ID)
                .WithDisplayName(NODE_INPUT_PREVIEW_TITLE)
                .Build();
        }

        // Output
        context.AddOutputPort<float[,]>(NODE_OUTPUT_GRID_ID)
            .WithDisplayName(NODE_OUTPUT_GRID_TITLE)
            .Build();
    }

    public bool TryValidateNode(GraphLogger graphLogger = null)
    {
        var isValid = true;

        PortEvaluator.TryEvaluateInputPort<float[,]>(this, NODE_INPUT_GRID_ID, _generationId, out var grid);
        if (grid == null)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID_TITLE} value missing", this);
            isValid = false;
        }

        return isValid;
    }

    public void ResetNode(int generationId)
    {
        _generationId = generationId;
        _cachedOutput = null;
    }

    public bool TryGetPortValue(IPort _, int generationId, out float[,] value)
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
            PortEvaluator.TryEvaluateInputPort<float[,]>(this, NODE_INPUT_GRID_ID, _generationId, out var grid);
            PortEvaluator.TryEvaluateInputPort<float>(this, NODE_INPUT_SEA_LEVEL_ID, _generationId, out var seaLevel);
            PortEvaluator.TryEvaluateInputPort<float>(this, NODE_INPUT_FALLOFF_WIDTH_ID, _generationId, out var falloffWidth);
            PortEvaluator.TryEvaluateInputPort<AnimationCurve>(this, NODE_INPUT_FALLOFF_CURVE_ID, _generationId, out var falloffCurve);

            int size = grid.GetLength(0);

            var output = new float[size, size];

            // Build binary sea mask: 0 = sea, 1 = land
            // (We’ll compute distances from sea pixels.)
            bool[,] isSea = new bool[size, size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    isSea[y, x] = (grid[y, x] <= seaLevel + SEA_EPSILON);
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
                    float height = grid[y, x];

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