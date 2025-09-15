using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[Serializable]
public class DisplaceSplineNode : ExecutableNode<SplineWrapper>
{
    private class InputValues
    {
        public SplineWrapper Spline;
        public IProvider Provider;
        public int VertexCount;
        public float Amplitude;
        public int IterationCount;

        public int VersionHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(Spline?.VersionHash, Provider?.VersionHash, VertexCount, Amplitude, IterationCount);
        }
    }

    // Options

    // Inputs
    private const string NODE_INPUT_SPLINE_ID = "spline_input";
    private const string NODE_INPUT_SPLINE_TITLE = "Spline";

    private const string NODE_INPUT_PROVIDER_ID = "provider_input";
    private const string NODE_INPUT_PROVIDER_TITLE = "Provider";

    private const string NODE_INPUT_VERTICES_ID = "vertices_input";
    private const string NODE_INPUT_VERTICES_TITLE = "Vertices";

    private const string NODE_INPUT_AMPLITUDE_ID = "amplitude_input";
    private const string NODE_INPUT_AMPLITUDE_TITLE = "Amplitude";

    private const string NODE_INPUT_ITERATIONS_ID = "iterations_input";
    private const string NODE_INPUT_ITERATIONS_TITLE = "Iterations";

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
        context.AddInputPort<SplineWrapper>(NODE_INPUT_SPLINE_ID)
            .WithDisplayName(NODE_INPUT_SPLINE_TITLE)
            .Build();
        context.AddInputPort<IProvider>(NODE_INPUT_PROVIDER_ID)
            .WithDisplayName(NODE_INPUT_PROVIDER_TITLE)
            .Build();
        context.AddInputPort<int>(NODE_INPUT_VERTICES_ID)
            .WithDisplayName(NODE_INPUT_VERTICES_TITLE)
            .WithDefaultValue(100)
            .Build();
        context.AddInputPort<float>(NODE_INPUT_AMPLITUDE_ID)
            .WithDisplayName(NODE_INPUT_AMPLITUDE_TITLE)
            .WithDefaultValue(30)
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

        if (input.Spline == null || !input.Spline.IsValid)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SPLINE_TITLE} value missing", this);
            isValid = false;
        }

        if (input.Provider == null || !input.Provider.IsValid)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_PROVIDER_TITLE} value missing", this);
            isValid = false;
        }
        else if (input.Provider is not INoiseProvider)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_PROVIDER_TITLE} value incorrect", this);
            isValid = false;
        }

        if (input.VertexCount < MIN_VERTEX_COUNT)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_VERTICES_TITLE} value invalid: {input.VertexCount} (valid: {MIN_VERTEX_COUNT} <= n)", this);
            isValid = false;
        }

        if (input.IterationCount <= 0)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_ITERATIONS_TITLE} value invalid: {input.IterationCount} (valid: 0 < n)", this);
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
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SPLINE_ID, out temp.Spline) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_PROVIDER_ID, out temp.Provider) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_VERTICES_ID, out temp.VertexCount) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_AMPLITUDE_ID, out temp.Amplitude) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_ITERATIONS_ID, out temp.IterationCount);

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
            var noiseProvider = inputValues.Provider as INoiseProvider;
            var inputSpline = inputValues.Spline;
            var vertexCount = inputValues.VertexCount;
            var amplitude = inputValues.Amplitude;
            var iterationCount = inputValues.IterationCount;

            var currentSpline = inputSpline.Spline;

            for (int i = 0; i < iterationCount; i++)
            {
                var vertices = new List<Vector3>();

                // Ignores the first and last vertex, so it remains anchored
                for (int vertexIndex = 1; vertexIndex < vertexCount; vertexIndex++)
                {
                    float t = vertexIndex / (float)vertexCount;

                    Vector3 position = currentSpline.EvaluatePosition(t);
                    Vector3 tangent = ((Vector3)currentSpline.EvaluateTangent(t)).normalized;

                    Vector3 up = Vector3.up;
                    Vector3 binormal = Vector3.Cross(up, tangent).normalized;

                    noiseProvider.TryGetNoise(new Vector2(t, t), out var noise);
                    Vector3 displacement = binormal * (noise - 0.5f) * amplitude;

                    Vector3 displacedPosition = position + displacement;
                    vertices.Add(displacedPosition);
                }

                var displacedSpline = new Spline(vertices.Select(v => (float3)v));
                displacedSpline.Closed = inputSpline.Spline.Closed;

                currentSpline = displacedSpline;
            }

            var bounds = currentSpline.GetBounds();
            var size = Mathf.CeilToInt(Mathf.Max(bounds.size.x, bounds.size.z));

            var outputSpline = new SplineWrapper
            {
                Spline = currentSpline,
                Size = size
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