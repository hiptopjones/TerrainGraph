using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class SmoothSplineNode : ExecutableNode<SplineWrapper>
    {
        private class InputValues
        {
            public SplineWrapper SplineWrapper;
            public int IterationCount;
            public float MinAngleDegrees;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(SplineWrapper?.VersionHash, IterationCount, MinAngleDegrees);
            }
        }

        // Options

        // Inputs
        private const string NODE_INPUT_SPLINE_ID = "spline_input";
        private const string NODE_INPUT_SPLINE_TITLE = "Spline";

        private const string NODE_INPUT_ITERATIONS_ID = "iterations_input";
        private const string NODE_INPUT_ITERATIONS_TITLE = "Iterations";

        private const string NODE_INPUT_ANGLE_ID = "angle_input";
        private const string NODE_INPUT_ANGLE_TITLE = "Min Angle";

        // Outputs
        private const string NODE_OUTPUT_SPLINE_ID = "spline_output";
        private const string NODE_OUTPUT_SPLINE_TITLE = "Spline";

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<bool>(NODE_OPTION_PREVIEW_ID)
                .WithDisplayName(NODE_OPTION_PREVIEW_TITLE)
                .WithDefaultValue(true)
                .Build();
        }

        private enum DisplacementAxis
        {
            Horizontal,
            Vertical,
            Both
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);

            // Input
            context.AddInputPort<SplineWrapper>(NODE_INPUT_SPLINE_ID)
                .WithDisplayName(NODE_INPUT_SPLINE_TITLE)
                .Build();
            context.AddInputPort<int>(NODE_INPUT_ITERATIONS_ID)
                .WithDisplayName(NODE_INPUT_ITERATIONS_TITLE)
                .WithDefaultValue(1)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_ANGLE_ID)
                .WithDisplayName(NODE_INPUT_ANGLE_TITLE)
                .WithDefaultValue(150)
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

            if (input.SplineWrapper == null || !input.SplineWrapper.IsValid)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SPLINE_TITLE} value missing", this);
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
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SPLINE_ID, out temp.SplineWrapper) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_ITERATIONS_ID, out temp.IterationCount) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_ANGLE_ID, out temp.MinAngleDegrees);

            temp.MinAngleDegrees = Mathf.Clamp(temp.MinAngleDegrees, 0, 180);

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
                var inputSplineWrapper = inputValues.SplineWrapper;
                var iterationCount = inputValues.IterationCount;
                var minAngleDegrees = inputValues.MinAngleDegrees;

                var currentSpline = inputSplineWrapper.Spline;

                for (int i = 0; i < iterationCount; i++)
                {
                    var vertices = new List<Vector3>();

                    int startIndex = 0;
                    int endIndex = currentSpline.Count - 1;

                    if (!currentSpline.Closed)
                    {
                        startIndex++;
                        endIndex--;

                        vertices.Add(currentSpline.First().Position);
                    }

                    for (int j = startIndex; j <= endIndex; j++)
                    {
                        var j1 = (j - 1 + currentSpline.Count) % currentSpline.Count;
                        var j2 = j;
                        var j3 = (j + 1) % currentSpline.Count;

                        var p1 = currentSpline[j1].Position;
                        var p2 = currentSpline[j2].Position;
                        var p3 = currentSpline[j3].Position;

                        var angleDegrees = Vector3.Angle(p1 - p2, p3 - p2);
                        if (angleDegrees < minAngleDegrees)
                        {
                            var midpoint = (p1 + p3) / 2;
                            var t = Mathf.InverseLerp(minAngleDegrees, 0, angleDegrees);
                            p2 = Vector3.Lerp(p2, midpoint, t);
                        }

                        vertices.Add(p2);
                    }

                    if (!currentSpline.Closed)
                    {
                        vertices.Add(currentSpline.Last().Position);
                    }

                    var smoothedSpline = SplineHelpers.CreateSpline(vertices, currentSpline.Closed);
                    var resampledSpline = SplineHelpers.ResampleSpline(smoothedSpline, currentSpline.Count);

                    currentSpline = resampledSpline;
                }

                var outputSpline = currentSpline;

                var outputSplineWrapper = new SplineWrapper
                {
                    Spline = outputSpline
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
}