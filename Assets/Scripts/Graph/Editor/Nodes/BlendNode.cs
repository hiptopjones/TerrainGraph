using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
public class BlendNode : Node,
    IValidatableNode,
    IEvaluatableNode<float[,]>,
    IPreviewableNode
{
    private int _generationId;
    private float[,] _cachedOutput;

    // Options
    private const string NODE_OPTION_PREVIEW_ID = "preview_option";
    private const string NODE_OPTION_PREVIEW_TITLE = "Enable Preview";

    // Input
    private const string NODE_INPUT_GRID1_ID = "grid1_input";
    private const string NODE_INPUT_GRID1_TITLE = "Grid 1";

    private const string NODE_INPUT_GRID2_ID = "grid2_input";
    private const string NODE_INPUT_GRID2_TITLE = "Grid 2";

    private const string NODE_INPUT_METHOD_ID = "method_input";
    private const string NODE_INPUT_METHOD_TITLE = "Method";

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
        context.AddInputPort<float[,]>(NODE_INPUT_GRID1_ID)
            .WithDisplayName(NODE_INPUT_GRID1_TITLE)
            .Build();
        context.AddInputPort<float[,]>(NODE_INPUT_GRID2_ID)
            .WithDisplayName(NODE_INPUT_GRID2_TITLE)
            .Build();
        context.AddInputPort<BlendMethod>(NODE_INPUT_METHOD_ID)
            .WithDisplayName(NODE_INPUT_METHOD_TITLE)
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

        PortEvaluator.TryEvaluateInputPort<float[,]>(this, NODE_INPUT_GRID1_ID, _generationId, out var grid1);
        if (grid1 == null)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID1_TITLE} value missing", this);
            isValid = false;
        }

        PortEvaluator.TryEvaluateInputPort<float[,]>(this, NODE_INPUT_GRID2_ID, _generationId, out var grid2);
        if (grid2 == null)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID2_TITLE} value missing", this);
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
            PortEvaluator.TryEvaluateInputPort<float[,]>(this, NODE_INPUT_GRID1_ID, _generationId, out var grid1);
            PortEvaluator.TryEvaluateInputPort<float[,]>(this, NODE_INPUT_GRID2_ID, _generationId, out var grid2);
            PortEvaluator.TryEvaluateInputPort<BlendMethod>(this, NODE_INPUT_METHOD_ID, _generationId, out var method);

            Func<float, float, float> blendFunction = null;

            switch (method)
            {
                case BlendMethod.Add:
                    blendFunction = (a, b) => a + b;
                    break;

                case BlendMethod.Multiply:
                    blendFunction = (a, b) => a * b;
                    break;

                default:
                    Debug.Log($"Unhandled blend method: {method}");
                    break;
            }

            // TODO: Validate all lengths are expected
            var size = grid1.GetLength(0);

            var output = new float[size, size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    output[x, y] = blendFunction(grid1[x, y], grid2[x, y]);
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