using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using static Indiecat.TerrainGraph.Editor.NodeConstants;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class SplineVoronoiHeightNode : ExecutableNode<HeightGrid>
    {
        private class InputValues
        {
            public bool IsSamplingEnabled;
            public SplineWrapper SplineWrapper;
            public int SampleCount;
            public int Size;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(SplineWrapper?.VersionHash, IsSamplingEnabled, SampleCount, Size);
            }
        }

        // Options
        private const string NODE_OPTION_SAMPLING_ID = "sample_option";
        private const string NODE_OPTION_SAMPLING_TITLE = "Use Sampled Points";

        // Inputs
        private const string NODE_INPUT_SPLINE_ID = "spline_input";
        private const string NODE_INPUT_SPLINE_TITLE = "Spline";

        private const string NODE_INPUT_SAMPLES_ID = "samples_input";
        private const string NODE_INPUT_SAMPLES_TITLE = "Sample Count";

        private const string NODE_INPUT_SIZE_ID = "size_input";
        private const string NODE_INPUT_SIZE_TITLE = "Size";

        // Outputs
        private const string NODE_OUTPUT_GRID_ID = "grid_output";
        private const string NODE_OUTPUT_GRID_TITLE = "Grid";

        // Other
        private const int MIN_SIZE = 16;
        private const int DEFAULT_SIZE = 256;

        private const int MIN_SAMPLE_COUNT = 10;
        private const int DEFAULT_SAMPLE_COUNT = 100;

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<bool>(NODE_OPTION_SAMPLING_ID)
                .WithDisplayName(NODE_OPTION_SAMPLING_TITLE)
                .WithDefaultValue(true)
                .Build();
            context.AddOption<bool>(NODE_OPTION_PREVIEW_ID)
                .WithDisplayName(NODE_OPTION_PREVIEW_TITLE)
                .WithDefaultValue(true)
                .Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);
            GetNodeOptionByName(NODE_OPTION_SAMPLING_ID).TryGetValue<bool>(out var isSamplingEnabled);

            // Input
            context.AddInputPort<SplineWrapper>(NODE_INPUT_SPLINE_ID)
                .WithDisplayName(NODE_INPUT_SPLINE_TITLE)
                .Build();

            if (isSamplingEnabled)
            {
                context.AddInputPort<int>(NODE_INPUT_SAMPLES_ID)
                    .WithDisplayName(NODE_INPUT_SAMPLES_TITLE)
                    .WithDefaultValue(DEFAULT_SAMPLE_COUNT)
                    .Build();
            }

            context.AddInputPort<int>(NODE_INPUT_SIZE_ID)
                .WithDisplayName(NODE_INPUT_SIZE_TITLE)
                .WithDefaultValue(DEFAULT_SIZE)
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

            if (input.IsSamplingEnabled && input.SampleCount < MIN_SAMPLE_COUNT)
            {
                if (graphLogger != null) graphLogger.LogWarning($"{NODE_INPUT_SAMPLES_TITLE} value invalid: {input.SampleCount} (valid: {MIN_SAMPLE_COUNT} <= n)", this);
                input.SampleCount = MIN_SAMPLE_COUNT;
            }

            if (input.Size < MIN_SIZE)
            {
                if (graphLogger != null) graphLogger.LogWarning($"{NODE_INPUT_SIZE_TITLE} value invalid: {input.Size} (valid: {MIN_SIZE} <= n)", this);
                input.Size = MIN_SIZE;
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
                GetNodeOptionByName(NODE_OPTION_SAMPLING_ID).TryGetValue(out temp.IsSamplingEnabled) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SPLINE_ID, out temp.SplineWrapper) &&
                (!temp.IsSamplingEnabled || PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SAMPLES_ID, out temp.SampleCount)) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SIZE_ID, out temp.Size);

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
            ComputeBuffer pointsBuffer = null;

            try
            {
                var inputSplineWrapper = inputValues.SplineWrapper;
                var isSamplingEnabled = inputValues.IsSamplingEnabled;
                var sampleCount = inputValues.SampleCount;
                var size = inputValues.Size;

                var inputSpline = inputSplineWrapper.Spline;

                List<Vector3> points = null;

                if (isSamplingEnabled)
                {
                    points = SplineHelpers.GetSplineVertices3d(inputSpline, sampleCount);
                }
                else
                {
                    points = inputSpline.Knots.Select(k => (Vector3)k.Position).ToList();
                }

                pointsBuffer = new ComputeBuffer(points.Count, sizeof(float) * 3);
                pointsBuffer.SetData(points);

                RenderTexture outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader($"Shaders/{nameof(SplineVoronoiHeightNode)}", out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetBuffer(kernel, "_Points", pointsBuffer);
                shader.SetFloat("_PointsCount", points.Count);
                shader.SetInt("_Size", size);

                var groups = Mathf.CeilToInt(size / 8.0f);
                shader.Dispatch(kernel, groups, groups, 1);

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
                if (pointsBuffer != null)
                {
                    pointsBuffer.Release();
                    pointsBuffer = null;
                }
            }
        }
    }
}