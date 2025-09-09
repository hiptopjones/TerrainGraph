using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
public class SplineMaskNode : Node,
    IValidatableNode,
    IEvaluatableNode<float[,]>,
    IPreviewableNode
{
    private bool _isNodeStateValid;
    private int _generationId;
    private float[,] _cachedOutput;

    // Options
    private const string NODE_OPTION_PREVIEW_ID = "preview_option";
    private const string NODE_OPTION_PREVIEW_TITLE = "Enable Preview";

    // Input
    private const string NODE_INPUT_SPLINE_ID = "spline_input";
    private const string NODE_INPUT_SPLINE_TITLE = "Spline";

    private const string NODE_INPUT_SIZE_ID = "size_input";
    private const string NODE_INPUT_SIZE_TITLE = "Size";

    private const string NODE_INPUT_STEP_ID = "step_input";
    private const string NODE_INPUT_STEP_TITLE = "Step";

    private const string NODE_INPUT_PREVIEW_ID = "preview_input";
    private const string NODE_INPUT_PREVIEW_TITLE = "Preview";

    // Output
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
        context.AddInputPort<SplineWrapper>(NODE_INPUT_SPLINE_ID)
            .WithDisplayName(NODE_INPUT_SPLINE_TITLE)
            .Build();
        context.AddInputPort<int>(NODE_INPUT_SIZE_ID)
            .WithDisplayName(NODE_INPUT_SIZE_TITLE)
            .WithDefaultValue(256)
            .Build();
        context.AddInputPort<float>(NODE_INPUT_STEP_ID)
            .WithDisplayName(NODE_INPUT_STEP_TITLE)
            .WithDefaultValue(10)
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

        PortEvaluator.TryEvaluateInputPort<SplineWrapper>(this, NODE_INPUT_SPLINE_ID, _generationId, out var splineWrapper);
        if (splineWrapper == null)
        {
            graphLogger.LogError($"{NODE_INPUT_SPLINE_TITLE} value missing", this);
            _isNodeStateValid = false;
        }

        PortEvaluator.TryEvaluateInputPort<int>(this, NODE_INPUT_SIZE_ID, _generationId, out var size);
        if (size <= 0)
        {
            graphLogger.LogError($"{NODE_INPUT_SIZE_TITLE} value invalid: {size} (valid: 0 < n)", this);
            _isNodeStateValid = false;
        }

        PortEvaluator.TryEvaluateInputPort<float>(this, NODE_INPUT_STEP_ID, _generationId, out var step);
        if (step <= 0)
        {
            graphLogger.LogError($"{NODE_INPUT_STEP_TITLE} value invalid: {step} (valid: 0 < n)", this);
            _isNodeStateValid = false;
        }
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
        if (!_isNodeStateValid)
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
            // Validation performed in ValidateNode()
            PortEvaluator.TryEvaluateInputPort<SplineWrapper>(this, NODE_INPUT_SPLINE_ID, _generationId, out var splineWrapper);
            PortEvaluator.TryEvaluateInputPort<int>(this, NODE_INPUT_SIZE_ID, _generationId, out var size);
            PortEvaluator.TryEvaluateInputPort<float>(this, NODE_INPUT_STEP_ID, _generationId, out var step);

            var spline = splineWrapper.Spline;

            var heights = new float[size, size];

            var vertices = SplineHelpers.GetSplineVertices(spline, step);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    heights[x, y] = 0;

                    if (GeometryHelpers.IsPointInPolygon(new Vector3(x, 0, y), vertices, performSanityCheck: true))
                    {
                        heights[x, y] = 1;
                    }
                }
            }

            _cachedOutput = heights;
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