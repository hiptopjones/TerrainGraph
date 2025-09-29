using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        private struct Segment
        {
            public Vector2 p1;
            public Vector2 p2;
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

        private const int MIN_VERTEX_COUNT = 10;

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
                .WithDefaultValue(0.3f)
                .Build();
            context.AddInputPort<int>(NODE_INPUT_INDEX_ID)
                .WithDisplayName(NODE_INPUT_INDEX_TITLE)
                .WithDefaultValue(0)
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

            if (input.Grid == null || !input.Grid.IsValid)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID_TITLE} value missing", this);
                isValid = false;
            }

            if (input.ContourIndex < 0)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_INDEX_TITLE} value invalid: {input.ContourIndex} (valid: 0 <= n)", this);
                isValid = false;
            }

            if (input.VertexCount <= MIN_VERTEX_COUNT)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_VERTICES_TITLE} value invalid: {input.VertexCount} (valid: {MIN_VERTEX_COUNT} < n)", this);
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
            ComputeBuffer segmentBuffer = null;
            ComputeBuffer counterBuffer = null;

            try
            {
                var inputGrid = inputValues.Grid;
                var contourHeight = inputValues.ContourHeight;
                var contourIndex = inputValues.ContourIndex;
                var vertexCount = inputValues.VertexCount;

                var size = inputGrid.Size;
                var maxSegmentCount = size * size;

                segmentBuffer = new ComputeBuffer(maxSegmentCount, sizeof(float) * 4, ComputeBufferType.Append | ComputeBufferType.Counter);
                counterBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

                var inputTexture = inputGrid.RenderTexture;

                if (!ComputeHelpers.TryLoadComputeShader($"Shaders/{nameof(ContourSplineNode)}", out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_InTexture", inputTexture);
                shader.SetInt("_Size", size);
                shader.SetFloat("_Height", contourHeight);
                shader.SetBuffer(kernel, "_OutSegments", segmentBuffer);
                shader.SetBuffer(kernel, "_SegmentCount", counterBuffer);

                var groups = Mathf.CeilToInt(size / 8.0f);
                shader.Dispatch(kernel, groups, groups, 1);

                ComputeBuffer.CopyCount(segmentBuffer, counterBuffer, 0);
                var countArray = new int[] { 0 };
                counterBuffer.GetData(countArray);
                var count = countArray[0];

                var segmentArray = new Segment[count];
                segmentBuffer.GetData(segmentArray, 0, 0, count);

                var segments = segmentArray.Select(s => new KeyValuePair<Vector2, Vector2>(s.p1, s.p2)).ToList();

                var contours = ContourDetector.GetContours(segments, contourHeight);
                if (contours == null || !contours.Any())
                {
                    Debug.LogError("Contours not detected");
                    return false;
                }

                if (contours.Count <= contourIndex)
                {
                    Debug.LogError($"Contour index invalid ({contours.Count} contours returned)");
                    return false;
                }

                var contour = contours.OrderByDescending(x => x.Count).Skip(contourIndex).First();
                var simplifiedContour = GeometryHelpers.SimplifyPolyline(contour, 2);
                //Debug.Log($"contour: {contour.Count} simplified: {simplifiedContour.Count}");

                var contourSpline = SplineHelpers.CreateSpline(simplifiedContour, closed: true);
                var outputSpline = SplineHelpers.ResampleSpline(contourSpline, vertexCount);

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
            finally
            {
                if (segmentBuffer != null)
                {
                    segmentBuffer.Release();
                    segmentBuffer = null;
                }

                if (counterBuffer != null)
                {
                    counterBuffer.Release();
                    counterBuffer = null;
                }
            }
        }
    }
}