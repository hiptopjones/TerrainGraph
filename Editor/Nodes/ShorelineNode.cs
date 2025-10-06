using Indiecat.TerrainGraph.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

[Serializable]
public class ShorelineNode : ExecutableNode<HeightGrid>
{
    private class InputValues
    {
        public SplineWrapper SplineWrapper;
        public int Size;
        public int SampleCount;
        public float StraightThreshold;

        public int VersionHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(SplineWrapper?.VersionHash, Size, SampleCount, StraightThreshold);
        }
    }

    // Options

    // Inputs
    private const string NODE_INPUT_SPLINE_ID = "spline_input";
    private const string NODE_INPUT_SPLINE_TITLE = "Spline";

    private const string NODE_INPUT_SIZE_ID = "size_input";
    private const string NODE_INPUT_SIZE_TITLE = "Size";

    private const string NODE_INPUT_SAMPLES_ID = "samples_input";
    private const string NODE_INPUT_SAMPLES_TITLE = "Samples";

    private const string NODE_INPUT_THRESHOLD_ID = "threshold_input";
    private const string NODE_INPUT_THRESHOLD_TITLE = "Threshold";

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
        context.AddInputPort<SplineWrapper>(NODE_INPUT_SPLINE_ID)
            .WithDisplayName(NODE_INPUT_SPLINE_TITLE)
            .Build();
        context.AddInputPort<int>(NODE_INPUT_SIZE_ID)
            .WithDisplayName(NODE_INPUT_SIZE_TITLE)
            .WithDefaultValue(256)
            .Build();
        context.AddInputPort<int>(NODE_INPUT_SAMPLES_ID)
            .WithDisplayName(NODE_INPUT_SAMPLES_TITLE)
            .WithDefaultValue(100)
            .Build();
        context.AddInputPort<float>(NODE_INPUT_THRESHOLD_ID)
            .WithDisplayName(NODE_INPUT_THRESHOLD_TITLE)
            .WithDefaultValue(0.1f)
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
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SIZE_ID, out temp.Size) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SAMPLES_ID, out temp.SampleCount) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_THRESHOLD_ID, out temp.StraightThreshold);

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
        Texture2D tempTexture = null;

        try
        {
            var inputSplineWrapper = inputValues.SplineWrapper;
            var size = inputValues.Size;
            var sampleCount = inputValues.SampleCount;
            var straightThreshold = inputValues.StraightThreshold;

            var inputSpline = inputSplineWrapper.Spline;
            var sampleDistance = inputSpline.GetLength() / sampleCount;

            if (!TryGetCurvatureSegments(inputSpline, sampleCount, straightThreshold, out var segments))
            {
                return false;
            }

            tempTexture = TextureHelpers.CreateTexture(size, size, TextureFormat.RFloat);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var position = new Vector2(x, y);

                    var height = 0f;

                    foreach (var segment in segments)
                    {
                        foreach (var segmentPosition in segment.Positions)
                        {
                            var radius = sampleDistance / 2;

                            var distance = (position - segmentPosition).magnitude;
                            if (distance <= radius)
                            {
                                switch (segment.Curvature)
                                {
                                    case CurvatureType.Concave:
                                        height = 100;
                                        break;
                                    case CurvatureType.Convex:
                                        height = -100;
                                        break;
                                    case CurvatureType.Straight:
                                        height = 1;
                                        break;
                                    case CurvatureType.Unknown:
                                        height = 0.5f;
                                        break;
                                }

                                break;
                            }
                        }
                    }

                    tempTexture.SetPixel(x, y, new Color(height, 0, 0));
                }
            }

            tempTexture.Apply();

            var outputTexture = GetOrCreateNodeRenderTexture(size);
            Graphics.Blit(tempTexture, outputTexture);

            var outputGrid = new HeightGrid(size);

            outputGrid.RenderTexture = outputTexture;
            outputGrid.VersionHash = inputValues.VersionHash;

            CacheData.Output = outputGrid;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
        finally
        {
            if (tempTexture != null)
            {
                Object.DestroyImmediate(tempTexture);
                tempTexture = null;
            }
        }
    }

    private enum CurvatureType
    {
        Unknown,
        Straight,
        Convex,
        Concave
    }

    private class CurvatureSegment
    {
        public CurvatureType Curvature;
        public float ArcLength;
        public int StartIndex;
        public int Count;
        public List<Vector2> Positions;
        public float AverageCross;
    }

    private static bool TryGetCurvatureSegments(Spline inputSpline, int sampleCount, float straightThreshold, out List<CurvatureSegment> segments)
    {
        segments = new List<CurvatureSegment>();

        if (!TryGetCurvatures(inputSpline, sampleCount, straightThreshold, out var curvatures, out var crosses, out var positions))
        {
            Debug.Log("Failed to get curvatures");
            return false;
        }

        var distances = new List<float>();

        distances.Add(0);

        for (int i = 1; i < positions.Count; i++)
        {
            var p1 = positions[i - 1];
            var p2 = positions[i];

            var distance = (p2 - p1).magnitude;

            distances.Add(distance);
        }

        var startIndex = 0;

        while (startIndex < sampleCount - 1)
        {
            var curvature = curvatures.Skip(startIndex).First();
            var count = curvatures.Skip(startIndex).TakeWhile(x => x == curvature).Count();

            var segment = new CurvatureSegment
            {
                Curvature = curvature,
                ArcLength = distances.Skip(startIndex).Take(count + 1).Sum(),
                StartIndex = startIndex,
                Count = count + 1,
                Positions = positions.Skip(startIndex).Take(count + 1).ToList(),
                AverageCross = crosses.Skip(startIndex).Take(count + 1).Average(),
            };

            segments.Add(segment);

            startIndex += count + 1;
        }

        return true;
    }

    private static bool TryGetCurvatures(Spline inputSpline, int sampleCount, float straightThreshold, out List<CurvatureType> curvatures, out List<float> crosses, out List<Vector2> positions)
    {
        curvatures = new List<CurvatureType>();
        crosses = new List<float>();
        positions = new List<Vector2>();

        for (int i = 0; i < sampleCount; i++)
        {
            var t = i / (float)(sampleCount - 1);

            Vector3 position = inputSpline.EvaluatePosition(t);
            positions.Add(new Vector2(position.x, position.z));
        }

        curvatures.Add(CurvatureType.Unknown);
        crosses.Add(0);

        // Cut off the ends
        for (int i = 1; i < sampleCount - 1; i++)
        {
            var p1 = positions[i - 1];
            var p2 = positions[i];
            var p3 = positions[i + 1];

            var cross = GeometryHelpers.Cross(p2 - p1, p3 - p2);
            crosses.Add(cross);

            var curvature = GetCurvature(cross, straightThreshold);
            curvatures.Add(curvature);
        }

        curvatures.Add(CurvatureType.Unknown);
        crosses.Add(0);

        return true;
    }

    private static CurvatureType GetCurvature(float cross, float straightThreshold)
    {
        if (Mathf.Abs(cross) < straightThreshold)
        {
            return CurvatureType.Straight;
        }
        else if (cross > 0)
        {
            return CurvatureType.Convex;
        }
        else
        {
            return CurvatureType.Concave;
        }
    }
}