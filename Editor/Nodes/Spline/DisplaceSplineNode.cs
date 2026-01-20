using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Splines;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class DisplaceSplineNode : ExecutableNode<SplineWrapper>
    {
        private enum DisplacementAxis
        {
            Horizontal,
            Vertical
        }

        private class InputValues
        {
            public DisplacementAxis DisplacementAxis;
            public SplineWrapper SplineWrapper;
            public float LinearOffset;
            public float Frequency;
            public float Amplitude;
            public int Seed;
            public int IterationCount;
            public int VertexCount;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(DisplacementAxis, SplineWrapper?.VersionHash, LinearOffset, Frequency, Amplitude, Seed, IterationCount, VertexCount);
            }
        }

        // Options
        private const string NODE_OPTION_AXIS_ID = "axis_input";
        private const string NODE_OPTION_AXIS_TITLE = "Axis";

        // Inputs
        private const string NODE_INPUT_SPLINE_ID = "spline_input";
        private const string NODE_INPUT_SPLINE_TITLE = "Spline";

        private const string NODE_INPUT_OFFSET_ID = "offset_input";
        private const string NODE_INPUT_OFFSET_TITLE = "Offset";

        private const string NODE_INPUT_FREQUENCY_ID = "frequency_input";
        private const string NODE_INPUT_FREQUENCY_TITLE = "Frequency";

        private const string NODE_INPUT_AMPLITUDE_ID = "amplitude_input";
        private const string NODE_INPUT_AMPLITUDE_TITLE = "Amplitude";

        private const string NODE_INPUT_SEED_ID = "seed_input";
        private const string NODE_INPUT_SEED_TITLE = "Seed";

        private const string NODE_INPUT_ITERATIONS_ID = "iterations_input";
        private const string NODE_INPUT_ITERATIONS_TITLE = "Iterations";

        private const string NODE_INPUT_VERTICES_ID = "vertices_input";
        private const string NODE_INPUT_VERTICES_TITLE = "Vertices";

        // Outputs
        private const string NODE_OUTPUT_SPLINE_ID = "spline_output";
        private const string NODE_OUTPUT_SPLINE_TITLE = "Spline";

        private const int MIN_VERTEX_COUNT = 10;

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<bool>(NODE_OPTION_PREVIEW_ID)
                .WithDisplayName(NODE_OPTION_PREVIEW_TITLE)
                .WithDefaultValue(true)
                .Build();
            context.AddOption<DisplacementAxis>(NODE_OPTION_AXIS_ID)
                .WithDisplayName(NODE_OPTION_AXIS_TITLE)
                .WithDefaultValue(DisplacementAxis.Horizontal)
                .Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);

            // Input
            context.AddInputPort<SplineWrapper>(NODE_INPUT_SPLINE_ID)
                .WithDisplayName(NODE_INPUT_SPLINE_TITLE)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_OFFSET_ID)
                .WithDisplayName(NODE_INPUT_OFFSET_TITLE)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_FREQUENCY_ID)
                .WithDisplayName(NODE_INPUT_FREQUENCY_TITLE)
                .WithDefaultValue(2f)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_AMPLITUDE_ID)
                .WithDisplayName(NODE_INPUT_AMPLITUDE_TITLE)
                .WithDefaultValue(30f)
                .Build();
            context.AddInputPort<int>(NODE_INPUT_SEED_ID)
                .WithDisplayName(NODE_INPUT_SEED_TITLE)
                .Build();
            context.AddInputPort<int>(NODE_INPUT_ITERATIONS_ID)
                .WithDisplayName(NODE_INPUT_ITERATIONS_TITLE)
                .WithDefaultValue(1)
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

            if (!Enum.IsDefined(typeof(DisplacementAxis), input.DisplacementAxis))
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_OPTION_AXIS_TITLE} option invalid", this);
                isValid = false;
            }

            if (input.SplineWrapper == null || !input.SplineWrapper.IsValid)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SPLINE_TITLE} value missing", this);
                isValid = false;
            }

            if (input.Frequency <= 0)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_FREQUENCY_TITLE} value invalid: {input.Frequency} (valid: 0 < n)", this);
                isValid = false;
            }

            if (input.Amplitude <= 0)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_AMPLITUDE_TITLE} value invalid: {input.Amplitude} (valid: 0 < n)", this);
                isValid = false;
            }

            if (input.IterationCount <= 0)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_ITERATIONS_TITLE} value invalid: {input.IterationCount} (valid: 0 < n)", this);
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
                GetNodeOptionByName(NODE_OPTION_AXIS_ID).TryGetValue(out temp.DisplacementAxis) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SPLINE_ID, out temp.SplineWrapper) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_OFFSET_ID, out temp.LinearOffset) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_FREQUENCY_ID, out temp.Frequency) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_AMPLITUDE_ID, out temp.Amplitude) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SEED_ID, out temp.Seed) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_ITERATIONS_ID, out temp.IterationCount) &&
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
                var displacementAxis = inputValues.DisplacementAxis;
                var inputSplineWrapper = inputValues.SplineWrapper;
                var offset = inputValues.LinearOffset;
                var frequency = inputValues.Frequency;
                var amplitude = inputValues.Amplitude;
                var seed = inputValues.Seed;
                var iterationCount = inputValues.IterationCount;
                var vertexCount = inputValues.VertexCount;

                var start = NoiseHelpers.GetOffsetPositionInternal(Vector2.zero, seed);

                var currentSpline = inputSplineWrapper.Spline;

                for (int i = 0; i < iterationCount; i++)
                {
                    var vertices = new List<Vector3>();

                    // TODO: Consider controlling whether the first and last vertex are displaced
                    for (int j = 0; j < vertexCount; j++)
                    {
                        var t = j / (float)(vertexCount - 1);
                        if (currentSpline.Closed)
                        {
                            // DO NOT add a vertex at t = 1 if it's closed
                            t = j / (float)vertexCount;
                        }

                        var position = currentSpline.EvaluatePosition(t);
                        var tangent = ((Vector3)currentSpline.EvaluateTangent(t)).normalized;

                        var up = Vector3.up;
                        var binormal = Vector3.Cross(up, tangent).normalized;

                        var displacement = Vector3.zero;

                        var noise = GetSeamlessNoise(start, frequency, t + offset);

                        switch (displacementAxis)
                        {
                            case DisplacementAxis.Horizontal:
                                displacement += binormal * noise * amplitude;
                                break;

                            case DisplacementAxis.Vertical:
                                displacement += up * Mathf.Clamp01(noise) * amplitude;
                                break;
                        }

                        var displacedPosition = (Vector3)position + displacement;
                        vertices.Add(displacedPosition);
                    }

                    var displacedSpline = SplineHelpers.CreateSpline(vertices, currentSpline.Closed);

                    currentSpline = displacedSpline;
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

        // Sample noise in a circle through the noise field, which makes it seamless
        public static float GetSeamlessNoise(Vector2 start, float frequency, float t)
        {
            var x = frequency * Mathf.Cos(t * 2 * Mathf.PI);
            var y = frequency * Mathf.Sin(t * 2 * Mathf.PI);

            var noise = NoiseHelpers.PerlinNoise2D(start.x + x, start.y + y);
            return noise;
        }
    }
}