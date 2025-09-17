using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[Serializable]
public class SpliceSplineNode : ExecutableNode<SplineWrapper>
{
    private class InputValues
    {
        public SplineWrapper SplineWrapper1;
        public SplineWrapper SplineWrapper2;
        public float Start;
        public float End;
        public int VertexCount;

        public int VersionHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(SplineWrapper1?.VersionHash, SplineWrapper2?.VersionHash, Start, End, VertexCount);
        }
    }

    // Options

    // Inputs
    private const string NODE_INPUT_SPLINE1_ID = "spline1_input";
    private const string NODE_INPUT_SPLINE1_TITLE = "Spline 1";

    private const string NODE_INPUT_SPLINE2_ID = "spline2_input";
    private const string NODE_INPUT_SPLINE2_TITLE = "Spline 2";

    private const string NODE_INPUT_START_ID = "start_input";
    private const string NODE_INPUT_START_TITLE = "Start";

    private const string NODE_INPUT_END_ID = "end_input";
    private const string NODE_INPUT_END_TITLE = "End";

    private const string NODE_INPUT_VERTICES_ID = "vertices_input";
    private const string NODE_INPUT_VERTICES_TITLE = "Vertices";

    // Outputs
    private const string NODE_OUTPUT_SPLINE_ID = "spline_output";
    private const string NODE_OUTPUT_SPLINE_TITLE = "Spline";

    private const int MIN_VERTEX_COUNT = 4;
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
        context.AddInputPort<SplineWrapper>(NODE_INPUT_SPLINE1_ID)
            .WithDisplayName(NODE_INPUT_SPLINE1_TITLE)
            .Build();
        context.AddInputPort<SplineWrapper>(NODE_INPUT_SPLINE2_ID)
            .WithDisplayName(NODE_INPUT_SPLINE2_TITLE)
            .Build();
        context.AddInputPort<float>(NODE_INPUT_START_ID)
            .WithDisplayName(NODE_INPUT_START_TITLE)
            .WithDefaultValue(0)
            .Build();
        context.AddInputPort<float>(NODE_INPUT_END_ID)
            .WithDisplayName(NODE_INPUT_END_TITLE)
            .WithDefaultValue(0.5f)
            .Build();
        context.AddInputPort<int>(NODE_INPUT_VERTICES_ID)
            .WithDisplayName(NODE_INPUT_VERTICES_TITLE)
            .WithDefaultValue(100)
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

        if (input.SplineWrapper1 == null || !input.SplineWrapper1.IsValid)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SPLINE1_TITLE} value missing", this);
            isValid = false;
        }

        if (input.SplineWrapper2 == null || !input.SplineWrapper2.IsValid)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SPLINE2_TITLE} value missing", this);
            isValid = false;
        }

        if (input.Start < 0)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_START_TITLE} value invalid: {input.Start} (valid: 0 <= n)", this);
            isValid = false;
        }

        if (input.End < 0)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_END_TITLE} value invalid: {input.End} (valid: 0 <= n)", this);
            isValid = false;
        }

        if (input.Start >= input.End)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_END_TITLE} value invalid: {input.End} (valid: start > end)", this);
            isValid = false;
        }

        if (input.VertexCount <= MIN_VERTEX_COUNT)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_VERTICES_TITLE} value invalid: {input.VertexCount} (valid: {MIN_VERTEX_COUNT} <= n)", this);
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
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SPLINE1_ID, out temp.SplineWrapper1) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SPLINE2_ID, out temp.SplineWrapper2) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_START_ID, out temp.Start) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_END_ID, out temp.End) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_VERTICES_ID, out temp.VertexCount);

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
            var inputSplineWrapper1 = inputValues.SplineWrapper1;
            var inputSplineWrapper2 = inputValues.SplineWrapper2;
            var vertexCount = inputValues.VertexCount;
            var start = inputValues.Start;
            var end = inputValues.End;

            var inputSpline1 = inputSplineWrapper1.Spline;
            var inputSpline2 = inputSplineWrapper2.Spline;

            // Prepare the partial spline for "seamless" splicing
            var transformedSpline2 = TransformSpline(inputSpline1, inputSpline2, start, end);

            var points = new List<float3>();

            var interval = 1 / (float)(vertexCount - 1);

            for (var t = 0f; t < start; t += interval)
            {
                var position = SplineUtility.EvaluatePosition(inputSpline1, t);
                points.Add(position);
            }

            for (var t1 = start; t1 < end; t1 += interval)
            {
                var t2 = Mathf.InverseLerp(start, end, t1);
                var position = SplineUtility.EvaluatePosition(transformedSpline2, t2);
                points.Add(position);
            }

            for (var t = end; t < 1f; t += interval)
            {
                var position = SplineUtility.EvaluatePosition(inputSpline1, t);
                points.Add(position);
            }

            var lastPosition = SplineUtility.EvaluatePosition(inputSpline1, 1);
            points.Add(lastPosition);

            var outputSpline = new Spline(points);
            outputSpline.Closed = inputSpline1.Closed;

            var bounds = outputSpline.GetBounds();
            var size = Mathf.CeilToInt(Mathf.Max(bounds.size.x, bounds.size.z));

            var outputSplineWrapper = new SplineWrapper
            {
                Spline = outputSpline,
                Size = size
            };

            outputSplineWrapper.VersionHash = inputValues.VersionHash;

            CacheData.Output = outputSplineWrapper;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
    }

    private Spline TransformSpline(Spline fullSpline, Spline partialSpline, float t1, float t2)
    {
        Vector3 fp1 = fullSpline.EvaluatePosition(t1);
        Vector3 fp2 = fullSpline.EvaluatePosition(t2);

        Vector3 pp1 = partialSpline.EvaluatePosition(0f);
        Vector3 pp2 = partialSpline.EvaluatePosition(1f);

        Vector3 fullDirection = (fp2 - fp1);
        float fullLength = fullDirection.magnitude;

        Vector3 partialDirection = (pp2 - pp1);
        float partialLength = partialDirection.magnitude;

        float scale = fullLength / partialLength;
        Quaternion rotation = Quaternion.FromToRotation(partialDirection, fullDirection);
        Vector3 offset = fp1 - rotation * (pp1 * scale);

        var transformedPoints = new List<Vector2>();
        foreach (var knot in partialSpline.Knots)
        {
            Vector3 transformedPosition = rotation * (knot.Position * scale) + offset;
            transformedPoints.Add(new Vector2(transformedPosition.x, transformedPosition.z));
        }

        return SplineHelpers.CreateSpline(transformedPoints);
    }
}