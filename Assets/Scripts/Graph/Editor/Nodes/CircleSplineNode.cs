using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
public class CircleSplineNode : ExecutableNode<SplineWrapper>
{
    private class InputValues
    {
        public float Radius;
        public int Points;

        public int VersionHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(Radius, Points);
        }
    }

    // Options
    
    // Inputs
    private const string NODE_INPUT_RADIUS_ID = "radius_input";
    private const string NODE_INPUT_RADIUS_TITLE = "Radius";

    private const string NODE_INPUT_POINTS_ID = "points_input";
    private const string NODE_INPUT_POINTS_TITLE = "Points";

    // Outputs
    private const string NODE_OUTPUT_SPLINE_ID = "spline_output";
    private const string NODE_OUTPUT_SPLINE_TITLE = "Spline";

    private const int MIN_POINTS = 4;

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
        context.AddInputPort<float>(NODE_INPUT_RADIUS_ID)
            .WithDisplayName(NODE_INPUT_RADIUS_TITLE)
            .WithDefaultValue(100f)
            .Build();
        context.AddInputPort<int>(NODE_INPUT_POINTS_ID)
            .WithDisplayName(NODE_INPUT_POINTS_TITLE)
            .WithDefaultValue(10)
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

        if (input.Radius <= 0)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_RADIUS_TITLE} value invalid: {input.Radius} (valid: 0 < n)", this);
            isValid = false;
        }

        if (input.Points < MIN_POINTS)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_POINTS_TITLE} value invalid: {input.Points} (valid: {MIN_POINTS} <= n)", this);
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
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_RADIUS_ID, out temp.Radius) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_POINTS_ID, out temp.Points);

        if (success)
        {
            temp.VersionHash = temp.GetHashCode();

            input = temp;
            return true;
        }

        return false;
    }

    public override bool TryGetOutputValue(IPort _, out SplineWrapper value)
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
            var radius = inputValues.Radius;
            var points = inputValues.Points;

            var center = Vector2.one * radius;
            var size = Mathf.RoundToInt(radius * 2);

            var interval = 360f / points;
            var spline = SplineFunctions.Circle(radius, interval, center);
        
            var outputSpline = new SplineWrapper
            {
                Size = size,
                Spline = spline
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