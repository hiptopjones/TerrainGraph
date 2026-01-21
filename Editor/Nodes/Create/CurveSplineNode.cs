using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using static Indiecat.TerrainGraph.Editor.NodeConstants;

namespace Indiecat.TerrainGraph.Editor
{
    using CurveType = CurveFunctions.CurveType;

    [Serializable]
    public class CurveSplineNode : ExecutableNode<SplineWrapper>
    {
        private class InputValues
        {
            public CurveType CurveType;
            public int Size;
            public int VertexCount;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(CurveType, Size, VertexCount);
            }
        }

        // Options
        private const string NODE_OPTION_TYPE_ID = "type_option";
        private const string NODE_OPTION_TYPE_TITLE = "Curve Type";

        // Inputs
        private const string NODE_INPUT_SIZE_ID = "size_input";
        private const string NODE_INPUT_SIZE_TITLE = "Size";

        private const string NODE_INPUT_VERTICES_ID = "vertices_input";
        private const string NODE_INPUT_VERTICES_TITLE = "Vertices";

        // Outputs
        private const string NODE_OUTPUT_SPLINE_ID = "spline_output";
        private const string NODE_OUTPUT_SPLINE_TITLE = "Spline";

        // Other
        private const int MIN_SIZE = 16;
        private const int DEFAULT_SIZE = 256;

        private const int MIN_VERTEX_COUNT = 10;
        private const int DEFAULT_VERTEX_COUNT = 10;

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<CurveType>(NODE_OPTION_TYPE_ID)
                .WithDisplayName(NODE_OPTION_TYPE_TITLE)
                .WithDefaultValue(CurveType.Line)
                .Build();
            context.AddOption<bool>(NODE_OPTION_PREVIEW_ID)
                .WithDisplayName(NODE_OPTION_PREVIEW_TITLE)
                .WithDefaultValue(true)
                .Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);

            // Input
            context.AddInputPort<int>(NODE_INPUT_SIZE_ID)
                .WithDisplayName(NODE_INPUT_SIZE_TITLE)
                .WithDefaultValue(DEFAULT_SIZE)
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

            if (!Enum.IsDefined(typeof(CurveType), input.CurveType))
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_OPTION_TYPE_TITLE} option invalid", this);
                isValid = false;
            }

            if (input.Size < MIN_SIZE)
            {
                if (graphLogger != null) graphLogger.LogWarning($"{NODE_INPUT_SIZE_TITLE} value invalid: {input.Size} (valid: {MIN_SIZE} <= n)", this);
                input.Size = MIN_SIZE;
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
                GetNodeOptionByName(NODE_OPTION_TYPE_ID).TryGetValue(out temp.CurveType) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SIZE_ID, out temp.Size) &&
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
                var curveType = inputValues.CurveType;
                var size = inputValues.Size;
                var vertexCount = inputValues.VertexCount;

                if (!TryGetSpline(curveType, size, vertexCount, out var outputSpline))
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

        public bool TryGetSpline(CurveType curveType, int size, int vertexCount, out Spline spline)
        {
            spline = null;

            if (!CurveFunctions.TryGetFunction(curveType, out var curveFunction))
            {
                return false;
            }

            var vertices = new List<Vector2>();

            for (int i = 0; i < vertexCount; i++)
            {
                var t = i / (float)(vertexCount - 1);
                var vertex = curveFunction(t) * size;
                vertices.Add(vertex);
            }

            spline = new Spline(vertices.Select(p => new float3(p.x, 0, p.y)));
            return true;
        }
    }
}