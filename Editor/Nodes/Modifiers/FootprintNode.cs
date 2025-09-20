using System;
using Unity.GraphToolkit.Editor;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[Serializable]
public class FootprintNode : ExecutableNode<HeightGrid>
{
    private class InputValues
    {
        public HeightGrid Grid;
        public SplineWrapper SplineWrapper;
        public float MaxDistance;
        public float MinDepth;

        public int VersionHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(Grid?.VersionHash, SplineWrapper?.VersionHash, MaxDistance, MinDepth);
        }
    }

    // Options

    // Inputs
    private const string NODE_INPUT_GRID_ID = "grid_input";
    private const string NODE_INPUT_GRID_TITLE = "Grid";

    private const string NODE_INPUT_SPLINE_ID = "spline_input";
    private const string NODE_INPUT_SPLINE_TITLE = "Spline";

    private const string NODE_INPUT_DISTANCE_ID = "distance_input";
    private const string NODE_INPUT_DISTANCE_TITLE = "Distance";

    private const string NODE_INPUT_DEPTH_ID = "depth_input";
    private const string NODE_INPUT_DEPTH_TITLE = "Depth";

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
        context.AddInputPort<HeightGrid>(NODE_INPUT_GRID_ID)
            .WithDisplayName(NODE_INPUT_GRID_TITLE)
            .Build();
        context.AddInputPort<SplineWrapper>(NODE_INPUT_SPLINE_ID)
            .WithDisplayName(NODE_INPUT_SPLINE_TITLE)
            .Build();
        context.AddInputPort<float>(NODE_INPUT_DISTANCE_ID)
            .WithDisplayName(NODE_INPUT_DISTANCE_TITLE)
            .WithDefaultValue(30)
            .Build();
        context.AddInputPort<float>(NODE_INPUT_DEPTH_ID)
            .WithDisplayName(NODE_INPUT_DEPTH_TITLE)
            .WithDefaultValue(0.3f)
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
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID_TITLE} value missing", this);
            isValid = false;
        }

        if (input.SplineWrapper == null || !input.SplineWrapper.IsValid)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SPLINE_TITLE} value missing", this);
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
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SPLINE_ID, out temp.SplineWrapper) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_DISTANCE_ID, out temp.MaxDistance) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_DEPTH_ID, out temp.MinDepth);

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
            var inputGrid = inputValues.Grid;
            var inputSplineWrapper = inputValues.SplineWrapper;
            var maxDistance = inputValues.MaxDistance;
            var minDepth = inputValues.MinDepth;

            var inputSpline = inputSplineWrapper.Spline;

            var size = inputGrid.Size;

            var sampleCount = (int)(inputSpline.GetLength() / 5);

            if (!TryCreateCenteredSdf(inputSpline, sampleCount, size, out var distances, out var nearestPositions))
            {
                return false;
            }

            var outputGrid = new HeightGrid(size);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var distance = distances[x, y];
                    if (distance > 0)
                    {
                        // Wrong side of the spline
                        outputGrid[x, y] = inputGrid[x, y];
                        continue;
                    }

                    var position = new Vector2(x, y);
                    var nearestPosition = nearestPositions[x, y].SwizzleXZ();

                    var direction = (nearestPosition - position).normalized;

                    const float SAMPLE_DISTANCE = 5f;

                    var source = nearestPosition + direction * SAMPLE_DISTANCE;
                    var height = GridHelpers.SafeIndex(inputGrid, source.x, source.y);

                    // SDF contains positive distances inside and negative outside
                    var positiveDistance = Mathf.Abs(distance);
                    var depth = -height * positiveDistance / SAMPLE_DISTANCE;

                    // Smooth the depth to an end value by some distance
                    var t = Mathf.InverseLerp(maxDistance / 2, maxDistance, positiveDistance);
                    depth = Mathf.SmoothStep(depth, minDepth, t);

                    outputGrid[x, y] = depth;
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

    public static bool TryCreateCenteredSdf(Spline spline, int samples, int size, out float[,] distances, out Vector3[,] nearestPositions)
    {
        var bounds = spline.GetBounds();

        var inputCenter = bounds.center;
        var outputCenter = new Vector3(size / 2f, 0, size / 2f);

        spline = new Spline(spline);
        for (int i = 0; i < spline.Count; i++)
        {
            var knot = spline[i];
            knot.Position += (float3)(outputCenter - inputCenter);
            spline[i] = knot;
        }

        return SplineSdfJobRunner.TryCreateSdf(spline, samples, size, out distances, out nearestPositions);
    }
}