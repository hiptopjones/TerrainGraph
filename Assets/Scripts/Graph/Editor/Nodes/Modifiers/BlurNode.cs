using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
public class BlurNode : ExecutableNode<HeightGrid>
{
    private class InputValues
    {
        public HeightGrid Grid;
        public int Radius;
        public int Iterations;

        public int VersionHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(Grid?.VersionHash, Radius, Iterations);
        }
    }

    // Options

    // Inputs
    private const string NODE_INPUT_GRID_ID = "grid_input";
    private const string NODE_INPUT_GRID_TITLE = "Grid";

    private const string NODE_INPUT_RADIUS_ID = "radius_input";
    private const string NODE_INPUT_RADIUS_TITLE = "Radius";

    private const string NODE_INPUT_ITERATIONS_ID = "iterations_input";
    private const string NODE_INPUT_ITERATIONS_TITLE = "Iterations";

    // Outputs
    private const string NODE_OUTPUT_GRID_ID = "grid_output";
    private const string NODE_OUTPUT_GRID_TITLE = "Grid";

    // Other
    private const int MAX_ITERATIONS = 50;

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
        context.AddInputPort<HeightGrid>(NODE_INPUT_GRID_ID)
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

        if (input.Grid == null || !input.Grid.IsValid)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID_TITLE} input missing", this);
            isValid = false;
        }

        if (input.Radius <= 0)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_RADIUS_TITLE} value invalid: {input.Radius} (valid: 0 < n)", this);
            isValid = false;
        }

        if (input.Iterations <= 0 || input.Iterations > MAX_ITERATIONS)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_ITERATIONS_TITLE} value invalid: {input.Iterations} (valid: 0 < n < {MAX_ITERATIONS})", this);
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
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID_ID, out temp.Grid) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_RADIUS_ID, out temp.Radius) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_ITERATIONS_ID, out temp.Iterations);

        if (success)
        {
            temp.VersionHash = temp.GetHashCode();

            input = temp;
            return true;
        }

        return false;
    }

    public override bool TryGetOutputValue(IPort _, out HeightGrid value)
    {
        if (!TryExecuteNode())
        {
            value = null;
            return false;
        }

        value = CacheData.Output;
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
            var inputGrid = inputValues.Grid;
            var radius = inputValues.Radius;
            var iterations = inputValues.Iterations;

            int size = inputGrid.Size;

            var sourceGrid = inputGrid;

            for (int i = 0; i < iterations; i++)
            {
                var tempGrid = new HeightGrid(size);
                var targetGrid = new HeightGrid(size);

                // Horizontal
                for (int y = 0; y < size; y++)
                {
                    float sum = 0f;
                    int count = 0;

                    for (int x = -radius; x <= radius; x++)
                    {
                        sum += GridHelpers.SafeIndex(sourceGrid, x, y);
                        count++;
                    }

                    for (int x = 0; x < size; x++)
                    {
                        tempGrid[x, y] = sum / count;

                        // slide window
                        float left = GridHelpers.SafeIndex(sourceGrid, x - radius, y);
                        float right = GridHelpers.SafeIndex(sourceGrid, x + 1 + radius, y);
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
                        sum += GridHelpers.SafeIndex(tempGrid, x, y);
                        count++;
                    }

                    for (int y = 0; y < size; y++)
                    {
                        targetGrid[x, y] = Mathf.Clamp01(sum / count);

                        float top = GridHelpers.SafeIndex(tempGrid, x, y - radius);
                        float bottom = GridHelpers.SafeIndex(tempGrid, x, y + 1 + radius);
                        sum += bottom - top;
                    }
                }

                sourceGrid = targetGrid;
            }

            var outputGrid = sourceGrid;

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