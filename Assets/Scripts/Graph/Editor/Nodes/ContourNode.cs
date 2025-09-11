using System;
using System.Linq;
using Unity.GraphToolkit.Editor;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Windows;

[Serializable]
public class ContourNode : Node,
    IValidatableNode,
    IEvaluatableNode<SplineWrapper>,
    IPreviewableNode
{
    private class InputValues
    {
        public HeightGrid Grid;
        public float ContourHeight;

        public int GenerationHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(Grid.GenerationHash, ContourHeight);
        }
    }

    private SplineWrapper _cachedOutputSpline;
    private int _previewGenerationHash;

    // Options
    private const string NODE_OPTION_PREVIEW_ID = "preview_option";
    private const string NODE_OPTION_PREVIEW_TITLE = "Enable Preview";

    // Input
    private const string NODE_INPUT_GRID_ID = "grid_input";
    private const string NODE_INPUT_GRID_TITLE = "Grid";

    private const string NODE_INPUT_HEIGHT_ID = "height_input";
    private const string NODE_INPUT_HEIGHT_TITLE = "Height";

    private const string NODE_INPUT_PREVIEW_ID = "preview_input";
    private const string NODE_INPUT_PREVIEW_TITLE = "Preview";

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

    public bool TryValidateNode(GraphLogger graphLogger = null)
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

        if (input.Grid == null || input.Grid.Values == null || input.Grid.Values.Length == 0)
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
            temp.GenerationHash = temp.GetHashCode();

            input = temp;
            return true;
        }

        return false;
    }

    public bool TryGetOutputValue(IPort _, out SplineWrapper spline)
    {
        if (!TryExecuteNode())
        {
            spline = null;
            return false;
        }

        spline = _cachedOutputSpline;
        return true;
    }

    private bool TryExecuteNode()
    {
        if (!TryGetValidatedInputValues(out var inputValues))
        {
            // Not in valid state
            _cachedOutputSpline = null;
            return false;
        }

        if (_cachedOutputSpline != null && _cachedOutputSpline.GenerationHash == inputValues.GenerationHash)
        {
            // Node is already up-to-date
            return true;
        }

        // Clear the cached values in case there's an early exit below
        _cachedOutputSpline = null;

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

            var outputSpline = new SplineWrapper
            {
                Size = inputGrid.Width,
                Spline = SplineHelpers.CreateSpline(contour, closed: true),
                GenerationHash = inputValues.GenerationHash
            };

            _cachedOutputSpline = outputSpline;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
    }

    public bool TryUpdatePreview()
    {
        // Ensure we're up-to-date. Needed for standalone nodes that have nobody else to poke them
        if (!TryExecuteNode())
        {
            // Node execution failed
            return false;
        }

        GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);
        if (!isPreviewEnabled)
        {
            // Force generation when next enabled
            _previewGenerationHash = 0;

            // Preview is disabled, treat as up-to-date
            return true;
        }

        if (_previewGenerationHash == _cachedOutputSpline.GenerationHash)
        {
            // Preview is already up-to-date
            return true;
        }

        if (PreviewHelpers.TryUpdatePreview(this, NODE_INPUT_PREVIEW_ID, _cachedOutputSpline))
        {
            // Cache generation value to avoid unnecessary updates
            _previewGenerationHash = _cachedOutputSpline.GenerationHash;

            // Preview was successfully updated
            return true;
        }

        // Preview could not be updated
        return false;
    }
}