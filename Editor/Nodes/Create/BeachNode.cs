using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Splines;

[Serializable]
public class BeachNode : ExecutableNode<SplineWrapper>
{
    private class InputValues
    {
        public int VertexCount;
        public Vector2 ThetaRange;
        public float A;
        public float B;
        public Vector2 Stretch; 

        public int VersionHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(VertexCount, ThetaRange, A, B, Stretch);
        }
    }

    // Options

    // Inputs
    private const string NODE_INPUT_VERTICES_ID = "vertices_input";
    private const string NODE_INPUT_VERTICES_TITLE = "Vertices";

    private const string NODE_INPUT_THETA_ID = "theta_input";
    private const string NODE_INPUT_THETA_TITLE = "Theta Range";

    private const string NODE_INPUT_A_ID = "a_input";
    private const string NODE_INPUT_A_TITLE = "r0";

    private const string NODE_INPUT_B_ID = "b_input";
    private const string NODE_INPUT_B_TITLE = "cot(alpha)";

    private const string NODE_INPUT_STRETCH_ID = "stretch_input";
    private const string NODE_INPUT_STRETCH_TITLE = "Stretch";

    // Outputs
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
        context.AddInputPort<int>(NODE_INPUT_VERTICES_ID)
            .WithDisplayName(NODE_INPUT_VERTICES_TITLE)
            .WithDefaultValue(300)
            .Build();
        context.AddInputPort<Vector2>(NODE_INPUT_THETA_ID)
            .WithDisplayName(NODE_INPUT_THETA_TITLE)
            .WithDefaultValue(new Vector2(231.5f, 267.5f))
            .Build();
        context.AddInputPort<float>(NODE_INPUT_A_ID)
            .WithDisplayName(NODE_INPUT_A_TITLE)
            .WithDefaultValue(0.91f)
            .Build();
        context.AddInputPort<float>(NODE_INPUT_B_ID)
            .WithDisplayName(NODE_INPUT_B_TITLE)
            .WithDefaultValue(1.61f)
            .Build(); ;
        context.AddInputPort<Vector2>(NODE_INPUT_STRETCH_ID)
            .WithDisplayName(NODE_INPUT_STRETCH_TITLE)
            .WithDefaultValue(new Vector2(3.3f, 0.67f))
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
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_VERTICES_ID, out temp.VertexCount) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_THETA_ID, out temp.ThetaRange) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_A_ID, out temp.A) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_B_ID, out temp.B) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_STRETCH_ID, out temp.Stretch);

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
            var vertexCount = inputValues.VertexCount;
            var thetaRange = inputValues.ThetaRange;
            var a = inputValues.A;
            var b = inputValues.B;
            var stretch = inputValues.Stretch;

            var minTheta = thetaRange.x;
            var maxTheta = thetaRange.y;

            var points = new List<Vector2>();

            for (int i = 0; i < vertexCount; i++)
            {
                float t = (float)i / (vertexCount - 1);
                float theta = Mathf.Lerp(minTheta, maxTheta, t) * Mathf.Deg2Rad;
                float r = a * Mathf.Exp(b * theta);

                float x = 300 + r * Mathf.Cos(theta) * stretch.x;
                float y = r * Mathf.Sin(theta) * stretch.y;

                points.Add(new Vector2(x, y));

                if (x > 256 || y > 256)
                {
                    break;
                }
            }

            var outputSpline = SplineHelpers.CreateSpline(points, closed: false);

            var outputSplineWrapper = new SplineWrapper
            {
                Spline = outputSpline,
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
}