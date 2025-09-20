using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Splines;

[Serializable]
public class ExtrudeSplineNode : ExecutableNode<HeightGrid>
{
    private class InputValues
    {
        public SplineWrapper SplineWrapper;
        public int Size;
        public float Width;

        public int VersionHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(SplineWrapper?.VersionHash, Size, Width);
        }
    }

    // Options

    // Input
    private const string NODE_INPUT_SPLINE_ID = "grid_input";
    private const string NODE_INPUT_SPLINE_TITLE = "Grid";

    private const string NODE_INPUT_WIDTH_ID = "width_input";
    private const string NODE_INPUT_WIDTH_TITLE = "Width";

    private const string NODE_INPUT_SIZE_ID = "size_input";
    private const string NODE_INPUT_SIZE_TITLE = "Size";

    // Output
    private const string NODE_OUTPUT_GRID_ID = "spline_output";
    private const string NODE_OUTPUT_GRID_TITLE = "Spline";

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
        context.AddInputPort<float>(NODE_INPUT_WIDTH_ID)
            .WithDisplayName(NODE_INPUT_WIDTH_TITLE)
            .WithDefaultValue(20)
            .Build();
        context.AddInputPort<int>(NODE_INPUT_SIZE_ID)
            .WithDisplayName(NODE_INPUT_SIZE_TITLE)
            .WithDefaultValue(256)
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

        if (input.SplineWrapper == null || !input.SplineWrapper.IsValid)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SPLINE_TITLE} value missing", this);
            isValid = false;
        }

        if (input.Size <= 0)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SIZE_TITLE} value invalid: {input.Size} (valid: 0 < n)", this);
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
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SPLINE_ID, out temp.SplineWrapper) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_WIDTH_ID, out temp.Width) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SIZE_ID, out temp.Size);

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

        var startTime = DateTime.Now;
        if (TryExecuteNodeInternal(inputValues))
        {
            CacheData.Output.ExecutionTime = (float)(DateTime.Now - startTime).TotalSeconds;
            return true;
        }

        return false;
    }

    private bool TryExecuteNodeInternal(InputValues inputValues)
    {
        try
        {
            var inputSplineWrapper = inputValues.SplineWrapper;
            var width = inputValues.Width;
            var size = inputValues.Size;

            var inputSpline = inputSplineWrapper.Spline;

            var samples = (int)inputSpline.GetLength();

            if (!SplineSdfJobRunner.TryCreateSdf(inputSpline, samples, size, out var distances, out _))
            {
                return false;
            }

            var outputGrid = new HeightGrid(size);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var distance = distances[x, y];

                    if (distance < -width || distance > width)
                    {
                        outputGrid[x, y] = 0;
                    }
                    else
                    {
                        outputGrid[x, y] = distance;
                    }
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