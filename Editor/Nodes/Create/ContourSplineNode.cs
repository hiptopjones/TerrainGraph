using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ContourSplineNode : ExecutableNode<SplineWrapper>
    {
        private class InputValues
        {
            public HeightGrid Grid;
            public float ContourHeight;
            public int ContourIndex;
            public int VertexCount;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(Grid?.VersionHash, ContourHeight, ContourIndex, VertexCount);
            }
        }

        // Options

        // Input
        private const string NODE_INPUT_GRID_ID = "grid_input";
        private const string NODE_INPUT_GRID_TITLE = "Grid";

        private const string NODE_INPUT_HEIGHT_ID = "height_input";
        private const string NODE_INPUT_HEIGHT_TITLE = "Height";

        private const string NODE_INPUT_INDEX_ID = "contour_input";
        private const string NODE_INPUT_INDEX_TITLE = "Contour Index";

        private const string NODE_INPUT_VERTICES_ID = "vertices_input";
        private const string NODE_INPUT_VERTICES_TITLE = "Vertices";

        // Output
        private const string NODE_OUTPUT_SPLINE_ID = "spline_output";
        private const string NODE_OUTPUT_SPLINE_TITLE = "Spline";

        // Other
        private const float DEFAULT_HEIGHT = 0.3f;

        private const int MIN_VERTEX_COUNT = 10;
        private const int DEFAULT_VERTEX_COUNT = 10;

        private const int MIN_CONTOUR_INDEX = 0;
        private const int DEFAULT_CONTOUR_INDEX = 0;

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
            context.AddInputPort<HeightGrid>(NODE_INPUT_GRID_ID)
                .WithDisplayName(NODE_INPUT_GRID_TITLE)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_HEIGHT_ID)
                .WithDisplayName(NODE_INPUT_HEIGHT_TITLE)
                .WithDefaultValue(DEFAULT_HEIGHT)
                .Build();
            context.AddInputPort<int>(NODE_INPUT_INDEX_ID)
                .WithDisplayName(NODE_INPUT_INDEX_TITLE)
                .WithDefaultValue(DEFAULT_CONTOUR_INDEX)
                .Build();
            context.AddInputPort<int>(NODE_INPUT_VERTICES_ID)
                .WithDisplayName(NODE_INPUT_VERTICES_TITLE)
                .WithDefaultValue(DEFAULT_VERTEX_COUNT)
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

            if (input.Grid == null || !input.Grid.IsValid)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID_TITLE} value missing", this);
                isValid = false;
            }

            if (input.ContourIndex < MIN_CONTOUR_INDEX)
            {
                if (graphLogger != null) graphLogger.LogWarning($"{NODE_INPUT_INDEX_TITLE} value invalid: {input.ContourIndex} (valid: {MIN_CONTOUR_INDEX} <= n)", this);
                input.ContourIndex = MIN_CONTOUR_INDEX;
            }

            if (input.VertexCount < MIN_VERTEX_COUNT)
            {
                if (graphLogger != null) graphLogger.LogWarning($"{NODE_INPUT_VERTICES_TITLE} value invalid: {input.VertexCount} (valid: {MIN_VERTEX_COUNT} <= n)", this);
                input.VertexCount = MIN_VERTEX_COUNT;
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
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_HEIGHT_ID, out temp.ContourHeight) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_INDEX_ID, out temp.ContourIndex) &&
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
                var inputGrid = inputValues.Grid;
                var contourHeight = inputValues.ContourHeight;
                var contourIndex = inputValues.ContourIndex;
                var vertexCount = inputValues.VertexCount;

                var size = inputGrid.Size;

                if (!ShaderWrappers.TryGenerateContour(inputGrid, contourHeight, contourIndex, vertexCount, size, out var outputSpline))
                {
                    return false;
                }

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
}