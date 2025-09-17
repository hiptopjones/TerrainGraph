using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Splines;

[Serializable]
public class SplineNode : ExecutableNode<SplineWrapper>
{
    private class InputValues
    {
        public IProvider Provider;
        public int VertexCount;
        
        public int VersionHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(
                HashCode.Combine(Provider?.VersionHash, VertexCount)
            );
        }
    }

    // Options

    // Inputs
    private const string NODE_INPUT_PROVIDER_ID = "provider_input";
    private const string NODE_INPUT_PROVIDER_TITLE = "Provider";

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
        context.AddInputPort<IProvider>(NODE_INPUT_PROVIDER_ID)
            .WithDisplayName(NODE_INPUT_PROVIDER_TITLE)
            .Build();
        context.AddInputPort<int>(NODE_INPUT_VERTICES_ID)
            .WithDisplayName(NODE_INPUT_VERTICES_TITLE)
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

        if (input.Provider == null || !input.Provider.IsValid)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_PROVIDER_TITLE} value missing", this);
            isValid = false;
        }
        else if (input.Provider is not ISplineProvider)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_PROVIDER_TITLE} value incorrect", this);
            isValid = false;
        }

        if (input.VertexCount < MIN_VERTEX_COUNT)
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
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_PROVIDER_ID, out temp.Provider) &&
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
            var splineProvider = inputValues.Provider as ISplineProvider;
            var count = inputValues.VertexCount;

            if (!splineProvider.TryGetSpline(count, out var outputSpline))
            {
                return false;
            }

            var bounds = outputSpline.GetBounds();
            var outputSplineSize = Mathf.CeilToInt(Mathf.Max(bounds.size.x, bounds.size.z));

            var outputSplineWrapper = new SplineWrapper
            {
                Spline = outputSpline,
                Size = outputSplineSize
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
