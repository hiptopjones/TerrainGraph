using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
public class HeightGridNode : Node,
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

    private const string NODE_INPUT_HEIGHT_ID = "height_input";
    private const string NODE_INPUT_HEIGHT_TITLE = "Height";

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
        context.AddInputPort<float>(NODE_INPUT_HEIGHT_ID)
            .WithDisplayName(NODE_INPUT_HEIGHT_TITLE)
            .WithDefaultValue(0.5f)
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

        PortEvaluator.TryEvaluateInputPort<float>(this, NODE_INPUT_HEIGHT_ID, _generationId, out var height);
        if (height < 0 || height > 1)
        {
            if (graphLogger != null) graphLogger.LogError($"Invalid {NODE_INPUT_HEIGHT_TITLE} specified: {height} (valid: 0 <= n <= 1)", this);
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
            PortEvaluator.TryEvaluateInputPort<float>(this, NODE_INPUT_HEIGHT_ID, _generationId, out var height);

            var output = new HeightGrid(size);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    output[x, y] = height;
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