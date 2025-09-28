using System;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class SplineHeightNode : ExecutableNode<HeightGrid>
    {
        private class InputValues
        {
            public SplineWrapper SplineWrapper;
            public int Samples;
            public bool IsCentered;
            public bool IncludeEdgeHeight;
            public int Size;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(SplineWrapper?.VersionHash, Samples, IsCentered, IncludeEdgeHeight, Size);
            }
        }

        // Options

        // Inputs
        private const string NODE_INPUT_SPLINE_ID = "spline_input";
        private const string NODE_INPUT_SPLINE_TITLE = "Spline";

        private const string NODE_INPUT_SAMPLES_ID = "samples_input";
        private const string NODE_INPUT_SAMPLES_TITLE = "Samples";

        private const string NODE_INPUT_CENTER_ID = "center_input";
        private const string NODE_INPUT_CENTER_TITLE = "Center";

        private const string NODE_INPUT_EDGE_ID = "edge_input";
        private const string NODE_INPUT_EDGE_TITLE = "Include Edge Height";

        private const string NODE_INPUT_SIZE_ID = "size_input";
        private const string NODE_INPUT_SIZE_TITLE = "Size";

        // Outputs
        private const string NODE_OUTPUT_GRID_ID = "grid_output";
        private const string NODE_OUTPUT_GRID_TITLE = "Grid";

        private const int MIN_SAMPLES = 10;

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<bool>(NODE_OPTION_PREVIEW_ID)
                .WithDisplayName(NODE_OPTION_PREVIEW_TITLE)
                .WithDefaultValue(true)
                .Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);

            // Input
            context.AddInputPort<SplineWrapper>(NODE_INPUT_SPLINE_ID)
                .WithDisplayName(NODE_INPUT_SPLINE_TITLE)
                .Build();
            context.AddInputPort<int>(NODE_INPUT_SAMPLES_ID)
                .WithDisplayName(NODE_INPUT_SAMPLES_TITLE)
                .WithDefaultValue(100)
                .Build();
            context.AddInputPort<bool>(NODE_INPUT_CENTER_ID)
                .WithDisplayName(NODE_INPUT_CENTER_TITLE)
                .WithDefaultValue(true)
                .Build();
            context.AddInputPort<bool>(NODE_INPUT_EDGE_ID)
                .WithDisplayName(NODE_INPUT_EDGE_TITLE)
                .WithDefaultValue(false)
                .Build();
            context.AddInputPort<int>(NODE_INPUT_SIZE_ID)
                .WithDisplayName(NODE_INPUT_SIZE_TITLE)
                .WithDefaultValue(256)
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

            if (input.Samples < MIN_SAMPLES)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SAMPLES_TITLE} value invalid: {input.Samples} (valid: n < {MIN_SAMPLES})", this);
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
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SAMPLES_ID, out temp.Samples) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_CENTER_ID, out temp.IsCentered) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_EDGE_ID, out temp.IncludeEdgeHeight) &&
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
                var sampleCount = inputValues.Samples;
                var isCentered = inputValues.IsCentered;
                var includeEdgeHeight = inputValues.IncludeEdgeHeight;
                var size = inputValues.Size;

                var inputSpline = inputSplineWrapper.Spline;

                var points = SplineHelpers.GetSplineVertices2d(inputSpline, sampleCount);
                if (isCentered)
                {
                    var splineCenter = SplineHelpers.GetCenter(inputSpline);
                    var gridCenter = Vector2.one * size / 2;

                    points = points.Select(p => p - splineCenter + gridCenter).ToList();
                }

                pointsBuffer = new ComputeBuffer(points.Count, sizeof(float) * 2);
                pointsBuffer.SetData(points);

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader("Shaders/SplineHeightNode", out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetBuffer(kernel, "_Points", pointsBuffer);
                shader.SetInt("_PointsCount", points.Count);
                shader.SetBool("_Closed", inputSpline.Closed);
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