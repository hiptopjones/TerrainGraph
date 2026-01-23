using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using static Indiecat.TerrainGraph.Editor.NodeConstants;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class SliceSplineNode : ExecutableNode<SplineWrapper>
    {
        private class InputValues
        {
            public SplineWrapper SplineWrapper;
            public float Start;
            public float End;
            public int VertexCount;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(SplineWrapper?.VersionHash, Start, End, VertexCount);
            }
        }

        // Options

        // Inputs
        private const string NODE_INPUT_SPLINE_ID = "spline_input";
        private const string NODE_INPUT_SPLINE_TITLE = "Spline";

        private const string NODE_INPUT_START_ID = "start_input";
        private const string NODE_INPUT_START_TITLE = "Start";

        private const string NODE_INPUT_END_ID = "end_input";
        private const string NODE_INPUT_END_TITLE = "End";

        private const string NODE_INPUT_VERTICES_ID = "vertices_input";
        private const string NODE_INPUT_VERTICES_TITLE = "Vertices";

        // Outputs
        private const string NODE_OUTPUT_SPLINE_ID = "spline_output";
        private const string NODE_OUTPUT_SPLINE_TITLE = "Spline";

        // Other
        private const float MIN_START = 0;
        private const float DEFAULT_START = 0;

        private const float MIN_END = 0;
        private const float DEFAULT_END = 0.5f;

        private const int MIN_VERTEX_COUNT = 10;
        private const int DEFAULT_VERTEX_COUNT = 100;

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<bool>(NODE_OPTION_PREVIEW_ID)
                .WithDisplayName(NODE_OPTION_PREVIEW_TITLE)
                .WithDefaultValue(true)
                .Build();
            context.AddOption<bool>(NODE_OPTION_DISABLE_ID)
                .WithDisplayName(NODE_OPTION_DISABLE_TITLE)
                .WithDefaultValue(false)
                .Build();
            context.AddOption<WarningBanner>(NODE_OPTION_WARNING_ID)
                .WithDisplayName(NODE_OPTION_WARNING_TITLE)
                .Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);

            // Input
            context.AddInputPort<SplineWrapper>(NODE_INPUT_SPLINE_ID)
                .WithDisplayName(NODE_INPUT_SPLINE_TITLE)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_START_ID)
                .WithDisplayName(NODE_INPUT_START_TITLE)
                .WithDefaultValue(DEFAULT_START)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_END_ID)
                .WithDisplayName(NODE_INPUT_END_TITLE)
                .WithDefaultValue(DEFAULT_END)
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
            GetNodeOptionByName(NODE_OPTION_DISABLE_ID).TryGetValue(out bool isNodeSkipped);
            NodeHelpers.TrySetWarningBanner(this, isNodeSkipped ? "DISABLED" : null);
            if (isNodeSkipped)
            {
                return true;
            }

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

            if (input.Start < MIN_START)
            {
                if (graphLogger != null) graphLogger.LogWarning($"{NODE_INPUT_START_TITLE} value invalid: {input.Start} (valid: {MIN_START} <= n)", this);
                input.Start = MIN_START;
            }

            if (input.End < MIN_END)
            {
                if (graphLogger != null) graphLogger.LogWarning($"{NODE_INPUT_END_TITLE} value invalid: {input.End} (valid: {MIN_END} <= n)", this);
                input.End = MIN_END;
            }

            if (input.Start >= input.End)
            {
                if (graphLogger != null) graphLogger.LogWarning($"{NODE_INPUT_END_TITLE} value invalid: {input.End} (valid: start > end)", this);
                input.End = input.Start + 0.001f;
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
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SPLINE_ID, out temp.SplineWrapper) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_START_ID, out temp.Start) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_END_ID, out temp.End) &&
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
            GetNodeOptionByName(NODE_OPTION_DISABLE_ID).TryGetValue(out bool isNodeDisabled);
            if (isNodeDisabled)
            {
                return PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SPLINE_ID, out value);
            }

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
                var vertexCount = inputValues.VertexCount;
                var start = inputValues.Start;
                var end = inputValues.End;

                var inputSpline = inputSplineWrapper.Spline;

                var points = new List<float3>();

                for (int i = 0; i < vertexCount; i++)
                {
                    float t = Mathf.Lerp(start, end, i / (float)(vertexCount - 1));

                    var position = SplineUtility.EvaluatePosition(inputSpline, t);
                    points.Add(position);
                }

                var outputSpline = new Spline(points);

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