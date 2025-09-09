using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
public class BoxBlurNode : Node,
    IValidatableNode,
    IEvaluatableNode<float[,]>,
    IPreviewableNode
{
    private int _generationId;
    private float[,] _cachedOutput;

    // Options
    private const string NODE_OPTION_PREVIEW_ID = "preview_option";
    private const string NODE_OPTION_PREVIEW_TITLE = "Enable Preview";

    // Inputs
    private const string NODE_INPUT_GRID_ID = "grid_input";
    private const string NODE_INPUT_GRID_TITLE = "Grid";

    private const string NODE_INPUT_RADIUS_ID = "radius_input";
    private const string NODE_INPUT_RADIUS_TITLE = "Radius";

    private const string NODE_INPUT_ITERATIONS_ID = "iterations_input";
    private const string NODE_INPUT_ITERATIONS_TITLE = "Iterations";

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
        context.AddInputPort<int>(NODE_INPUT_RADIUS_ID)
            .WithDisplayName(NODE_INPUT_RADIUS_TITLE)
            .WithDefaultValue(1)
            .Build();
        context.AddInputPort<int>(NODE_INPUT_ITERATIONS_ID)
            .WithDisplayName(NODE_INPUT_ITERATIONS_TITLE)
            .WithDefaultValue(1)
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
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID_TITLE} input missing", this);
            isValid = false;
        }

        PortEvaluator.TryEvaluateInputPort<int>(this, NODE_INPUT_RADIUS_ID, _generationId, out var radius);
        if (radius <= 0 || radius > 10)
        {
            if (graphLogger != null) graphLogger.LogError($"Invalid {NODE_INPUT_RADIUS_TITLE} specified: {radius} (valid: 0 < n)", this);
            isValid = false;
        }

        PortEvaluator.TryEvaluateInputPort<int>(this, NODE_INPUT_ITERATIONS_ID, _generationId, out var iterations);
        if (iterations <= 0 || iterations > 10)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_ITERATIONS_TITLE} value invalid: {iterations} (valid: 0 < n < 10)", this);
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
            PortEvaluator.TryEvaluateInputPort<int>(this, NODE_INPUT_RADIUS_ID, _generationId, out var radius);
            PortEvaluator.TryEvaluateInputPort<int>(this, NODE_INPUT_ITERATIONS_ID, _generationId, out var iterations);

            int size = grid.GetLength(0);

            var output = new float[size, size];

            var tmp = new float[size, size];

            for (int it = 0; it < iterations; it++)
            {
                // Horizontal
                for (int y = 0; y < size; y++)
                {
                    float sum = 0f;
                    int count = 0;

                    for (int x = -radius; x <= radius; x++)
                    {
                        sum += SafeGet(grid, x, y);
                        count++;
                    }

                    for (int x = 0; x < size; x++)
                    {
                        tmp[x, y] = sum / count;

                        // slide window
                        float left = SafeGet(grid, x - radius, y);
                        float right = SafeGet(grid, x + 1 + radius, y);
                        sum += right - left;
                    }
                }

                // Vertical
                for (int x = 0; x < size; x++)
                {
                    float sum = 0f;
                    int count = 0;

                    for (int y = -radius; y <= radius; y++)
                    {
                        sum += SafeGet(tmp, x, y);
                        count++;
                    }

                    for (int y = 0; y < size; y++)
                    {
                        output[x, y] = sum / count;

                        float top = SafeGet(tmp, x, y - radius);
                        float bottom = SafeGet(tmp, x, y + 1 + radius);
                        sum += bottom - top;
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

    protected static float SafeGet(float[,] d, int x, int y)
    {
        int w = d.GetLength(0);
        int h = d.GetLength(1);
        x = Mathf.Clamp(x, 0, w - 1);
        y = Mathf.Clamp(y, 0, h - 1);
        return d[x, y];
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