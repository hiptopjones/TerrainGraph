using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public class NoiseMaskNode : ExecutableNode<HeightGrid>
{
    private class InputValues
    {
        public int Size;
        public NoiseProvider Noise;
        public Vector2 Range;
        public float Scale;

        public int VersionHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(
                HashCode.Combine(Size, Noise, Range, Scale)
            );
        }
    }

    // Options

    // Inputs
    private const string NODE_INPUT_NOISE_ID = "noise_input";
    private const string NODE_INPUT_NOISE_TITLE = "Noise";

    private const string NODE_INPUT_SIZE_ID = "size_input";
    private const string NODE_INPUT_SIZE_TITLE = "Size";

    private const string NODE_INPUT_RANGE_ID = "range_input";
    private const string NODE_INPUT_RANGE_TITLE = "Range";

    private const string NODE_INPUT_SCALE_ID = "scale_input";
    private const string NODE_INPUT_SCALE_TITLE = "Scale";

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
        context.AddInputPort<NoiseProvider>(NODE_INPUT_NOISE_ID)
            .WithDisplayName(NODE_INPUT_NOISE_TITLE)
            .Build();
        context.AddInputPort<int>(NODE_INPUT_SIZE_ID)
            .WithDisplayName(NODE_INPUT_SIZE_TITLE)
            .WithDefaultValue(256)
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

    public override bool TryValidateNode(GraphLogger graphLogger = null)
    {
        return TryGetValidatedInputValues(out _, graphLogger);
    }

    private bool TryGetValidatedInputValues(out InputValues validatedInput, GraphLogger graphLogger = null)
    {
        validatedInput = null;

        if (!TryGetInputValues(out var input))
        {
            if (graphLogger != null) graphLogger.LogError("Upstream failure", this);
            return false;
        }

        var isValid = true;

        if (input.Size <= 0)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SIZE_TITLE} value invalid: {input.Size} (valid: 0 < n)", this);
            isValid = false;
        }

        if (input.Noise == null || !input.Noise.IsValid)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_NOISE_TITLE} value missing", this);
            isValid = false;
        }

        if (isValid)
        {
            validatedInput = input;
        }

        return isValid;
    }

    private bool TryGetInputValues(out InputValues input)
    {
        input = null;

        var temp = new InputValues();
        var success =
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_NOISE_ID, out temp.Noise) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SIZE_ID, out temp.Size) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_RANGE_ID, out temp.Range) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SCALE_ID, out temp.Scale);

        if (success)
        {
            temp.VersionHash = temp.GetHashCode();

            input = temp;
            return true;
        }

        return false;
    }

    public override bool TryGetOutputValue(IPort _, out HeightGrid grid)
    {
        if (!TryExecuteNode())
        {
            grid = null;
            return false;
        }

        grid = CacheData.Output;
        return true;
    }

    public override bool TryExecuteNode()
    {
        if (!TryGetValidatedInputValues(out var inputValues))
        {
            // Not in valid state
            CacheData.Output = null;
            return false;
        }

        if (CacheData.Output != null && CacheData.Output.VersionHash == inputValues.VersionHash)
        {
            // Node is already up-to-date
            return true;
        }

        // Clear the cached values in case there's an early exit below
        CacheData.Output = null;

        try
        {
            var provider = inputValues.Noise;
            var size = inputValues.Size;
            var range = inputValues.Range;
            var scale = inputValues.Scale;

            var noise = provider.GetNoiseArray2D(Vector2.zero, size);

            var outputGrid = new HeightGrid(size);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var height = noise[x, y];
                    height = range.x + (range.y - range.x) * height;

                    outputGrid[x, y] = height * scale;
                }
            }

            outputGrid.VersionHash = inputValues.VersionHash;

            CacheData.Output = outputGrid;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
    }
}
