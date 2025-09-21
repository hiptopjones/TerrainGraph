using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class OffsetSplineNode : ExecutableNode<SplineWrapper>
    {
        private class InputValues
        {
            public SplineWrapper SplineWrapper;
            public int VertexCount;
            public float Offset;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(SplineWrapper?.VersionHash, VertexCount, Offset);
            }
        }

        // Options

        // Inputs
        private const string NODE_INPUT_SPLINE_ID = "spline_input";
        private const string NODE_INPUT_SPLINE_TITLE = "Spline";

        private const string NODE_INPUT_VERTICES_ID = "vertices_input";
        private const string NODE_INPUT_VERTICES_TITLE = "Vertices";

        private const string NODE_INPUT_OFFSET_ID = "offset_input";
        private const string NODE_INPUT_OFFSET_TITLE = "Offset";

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
            context.AddInputPort<int>(NODE_INPUT_VERTICES_ID)
                .WithDisplayName(NODE_INPUT_VERTICES_TITLE)
                .WithDefaultValue(100)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_OFFSET_ID)
                .WithDisplayName(NODE_INPUT_OFFSET_TITLE)
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

            if (input.SplineWrapper == null || !input.SplineWrapper.IsValid)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SPLINE_TITLE} value missing", this);
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
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SPLINE_ID, out temp.SplineWrapper) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_VERTICES_ID, out temp.VertexCount) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_OFFSET_ID, out temp.Offset);

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
                var vertexCount = inputValues.VertexCount;
                var offset = inputValues.Offset;

                var inputSpline = inputSplineWrapper.Spline;

                var vertices = new List<Vector3>();

                for (int i = 0; i < vertexCount; i++)
                {
                    var t = i / (float)(vertexCount - 1);

                    if (!inputSpline.Evaluate(t, out var vertex, out var tangent, out var _))
                    {
                        Debug.Log($"Failed to evaluate spline at {t}");
                        return false;
                    }

                    var binormal = Vector3.Cross(tangent, Vector3.up).normalized;

                    var offsetVertex = vertex + binormal * offset;

                    vertices.Add(offsetVertex);
                }

                var outputSpline = SplineHelpers.CreateSpline(vertices, closed: inputSpline.Closed);

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