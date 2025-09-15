using System;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Splines;

[Serializable]
public class ContourNode : ExecutableNode<SplineWrapper>
{
    private class InputValues
    {
        public HeightGrid Grid;
        public float ContourHeight;

        public int VersionHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(Grid.VersionHash, ContourHeight);
        }
    }

    // Options
    
    // Input
    private const string NODE_INPUT_GRID_ID = "grid_input";
    private const string NODE_INPUT_GRID_TITLE = "Grid";

    private const string NODE_INPUT_HEIGHT_ID = "height_input";
    private const string NODE_INPUT_HEIGHT_TITLE = "Height";

    // Output
    private const string NODE_OUTPUT_SPLINE_ID = "spline_output";
    private const string NODE_OUTPUT_SPLINE_TITLE = "Spline";

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
        context.AddInputPort<float>(NODE_INPUT_HEIGHT_ID)
            .WithDisplayName(NODE_INPUT_HEIGHT_TITLE)
            .WithDefaultValue(0.3f)
            .Build();

        if (isPreviewEnabled)
        {
            context.AddInputPort<PreviewImage>(NODE_INPUT_PREVIEW_ID)
                .WithDisplayName(NODE_INPUT_PREVIEW_TITLE)
                .Build();
        }

        // Output
        context.AddOutputPort<SplineWrapper>(NODE_OUTPUT_SPLINE_ID)
            .WithDisplayName(NODE_OUTPUT_SPLINE_TITLE)
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
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID_TITLE} value missing", this);
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
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_HEIGHT_ID, out temp.ContourHeight);

        if (success)
        {
            temp.VersionHash = temp.GetHashCode();

            input = temp;
            return true;
        }

        return false;
    }

    public override bool TryGetOutputValue(IPort _, out SplineWrapper spline)
    {
        if (!TryExecuteNode())
        {
            spline = null;
            return false;
        }

        spline = CacheData.Output;
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
            var contourHeight = inputValues.ContourHeight;

            var detector = new ContourDetector(inputGrid);

            var contours = detector.DetectContours(contourHeight);
            if (contours == null || !contours.Any())
            {
                Debug.LogError("Contours not detected");
                return false;
            }

            var contour = contours.OrderByDescending(x => x.Count).First();

            // TODO: Should apply some Douglas-Peucker smoothing to reduce points
            var spline = SplineHelpers.CreateSpline(contour, closed: true);

            var bounds = spline.GetBounds();
            var size = Mathf.CeilToInt(Mathf.Max(bounds.size.x, bounds.size.z));

            var outputSpline = new SplineWrapper
            {
                Size = size,
                Spline = spline,
            };

            outputSpline.VersionHash = inputValues.VersionHash;

            CacheData.Output = outputSpline;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
    }
}