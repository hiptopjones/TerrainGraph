using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Windows;

public class BoxBlurNode : Node, IValidatedNode, IEvaluatedNode<float[,]>
{
    private bool _isNodeStateValid;
    private float[,] _cachedOutput;

    // Options
    internal const string NODE_OPTION_PREVIEW_ID = TerrainEditorGraph.NODE_OPTION_PREVIEW_ID;

    private const string NODE_OPTION_PREVIEW_TITLE = "Enable Preview";

    // Inputs
    internal const string NODE_INPUT_GRID_ID = "grid_input";
    internal const string NODE_INPUT_RADIUS_ID = "radius_input";
    internal const string NODE_INPUT_ITERATIONS_ID = "iterations_input";
    internal const string NODE_INPUT_PREVIEW_ID = TerrainEditorGraph.NODE_INPUT_PREVIEW_ID;

    private const string NODE_INPUT_GRID_TITLE = "Grid";
    private const string NODE_INPUT_RADIUS_TITLE = "Radius";
    private const string NODE_INPUT_ITERATIONS_TITLE = "Iterations";
    private const string NODE_INPUT_PREVIEW_TITLE = "Preview";

    // Outputs
    internal const string NODE_OUTPUT_GRID_ID = TerrainEditorGraph.NODE_OUTPUT_GRID_ID;

    private const string NODE_OUTPUT_GRID_TITLE = "Grid";

    public override void OnEnable()
    {
        ResetNode();
    }

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

    public void ValidateNode(GraphLogger graphLogger)
    {
        _isNodeStateValid = true;

        var iterations = PortEvaluator.EvaluatePort<int>(GetInputPortByName(NODE_INPUT_ITERATIONS_ID));
        if (iterations <= 0 || iterations > 10)
        {
            graphLogger.LogError($"{NODE_INPUT_ITERATIONS_TITLE} value invalid: {iterations} (valid: 0 < n < 10)", this);
            _isNodeStateValid = false;
        }
        
        var radius = PortEvaluator.EvaluatePort<int>(GetInputPortByName(NODE_INPUT_RADIUS_ID));
        if (radius <= 0 || radius > 10)
        {
            graphLogger.LogError($"Invalid {NODE_INPUT_RADIUS_TITLE} specified: {radius} (valid: 0 < n)", this);
            _isNodeStateValid = false;
        }

        var input = PortEvaluator.EvaluatePort<float[,]>(GetInputPortByName(NODE_INPUT_GRID_ID));
        if (input == null)
        {
            graphLogger.LogError($"{NODE_INPUT_GRID_TITLE} input missing", this);
            _isNodeStateValid = false;
        }
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
        // Depends on ValidateNode() for validation
        if (!_isNodeStateValid)
        {
            return false;
        }

        try
        {
            var input = PortEvaluator.EvaluatePort<float[,]>(GetInputPortByName(NODE_INPUT_GRID_ID));
            var radius = PortEvaluator.EvaluatePort<int>(GetInputPortByName(NODE_INPUT_RADIUS_ID));
            var iterations = PortEvaluator.EvaluatePort<int>(GetInputPortByName(NODE_INPUT_ITERATIONS_ID));

            int size = input.GetLength(0);

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
                        sum += SafeGet(input, x, y);
                        count++;
                    }

                    for (int x = 0; x < size; x++)
                    {
                        tmp[x, y] = sum / count;

                        // slide window
                        float left = SafeGet(input, x - radius, y);
                        float right = SafeGet(input, x + 1 + radius, y);
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
}