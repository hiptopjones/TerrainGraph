using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Splines;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class OpenClosedSplineNode : ExecutableNode<SplineWrapper>
    {
        private enum OpenCloseOperation
        {
            OpenSpline,
            CloseSpline
        }

        private class InputValues
        {
            public OpenCloseOperation Operation;
            public SplineWrapper SplineWrapper;
            public bool AddLastVertex;
            public bool RemoveLastVertex;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(Operation, SplineWrapper?.VersionHash, AddLastVertex, RemoveLastVertex);
            }
        }

        // Options
        private const string NODE_OPTION_OPERATION_ID = "operation_input";
        private const string NODE_OPTION_OPERATION_TITLE = "Operation";

        // Inputs
        private const string NODE_INPUT_SPLINE_ID = "spline_input";
        private const string NODE_INPUT_SPLINE_TITLE = "Spline";

        private const string NODE_INPUT_ADD_ID = "add_input";
        private const string NODE_INPUT_ADD_TITLE = "Add Last Vertex";

        private const string NODE_INPUT_REMOVE_ID = "remove_input";
        private const string NODE_INPUT_REMOVE_TITLE = "Remove Last Vertex";

        // Outputs
        private const string NODE_OUTPUT_SPLINE_ID = "spline_output";
        private const string NODE_OUTPUT_SPLINE_TITLE = "Spline";

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<bool>(NODE_OPTION_PREVIEW_ID)
                .WithDisplayName(NODE_OPTION_PREVIEW_TITLE)
                .WithDefaultValue(false)
                .Build();

            context.AddOption<OpenCloseOperation>(NODE_OPTION_OPERATION_ID)
                .WithDisplayName(NODE_OPTION_OPERATION_TITLE)
                .WithDefaultValue(OpenCloseOperation.OpenSpline)
                .Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);
            GetNodeOptionByName(NODE_OPTION_OPERATION_ID).TryGetValue<OpenCloseOperation>(out var operation);

            // Input
            context.AddInputPort<SplineWrapper>(NODE_INPUT_SPLINE_ID)
                .WithDisplayName(NODE_INPUT_SPLINE_TITLE)
                .Build();

            if (operation == OpenCloseOperation.OpenSpline)
            {
                context.AddInputPort<bool>(NODE_INPUT_ADD_ID)
                    .WithDisplayName(NODE_INPUT_ADD_TITLE)
                    .WithDefaultValue(true)
                    .Build();
            }
            else
            {
                context.AddInputPort<bool>(NODE_INPUT_REMOVE_ID)
                    .WithDisplayName(NODE_INPUT_REMOVE_TITLE)
                    .WithDefaultValue(true)
                    .Build();
            }

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

            if (!Enum.IsDefined(typeof(OpenCloseOperation), input.Operation))
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_OPTION_OPERATION_ID} option invalid", this);
                isValid = false;
            }

            if (input.SplineWrapper == null || !input.SplineWrapper.IsValid)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SPLINE_TITLE} value missing", this);
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
                GetNodeOptionByName(NODE_OPTION_OPERATION_ID).TryGetValue(out temp.Operation) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SPLINE_ID, out temp.SplineWrapper) &&
                (temp.Operation != OpenCloseOperation.OpenSpline || PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_ADD_ID, out temp.AddLastVertex)) &&
                (temp.Operation != OpenCloseOperation.CloseSpline || PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_REMOVE_ID, out temp.RemoveLastVertex));

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
                var isClosingSpline = inputValues.Operation == OpenCloseOperation.CloseSpline;
                var addLastVertex = inputValues.AddLastVertex;
                var removeLastVertex = inputValues.RemoveLastVertex;

                var inputSpline = inputSplineWrapper.Spline;

                var vertices = inputSpline.Knots.Select(k => k.Position).ToList();

                Spline outputSpline;

                if (isClosingSpline)
                {
                    if (inputSpline.Closed)
                    {
                        outputSpline = inputSpline;
                    }
                    else
                    {
                        if (removeLastVertex)
                        {
                            vertices.RemoveAt(vertices.Count - 1);
                        }

                        outputSpline = SplineHelpers.CreateSpline(vertices, closed: true);
                    }
                }
                else
                {
                    if (!inputSpline.Closed)
                    {
                        outputSpline = inputSpline;
                    }
                    else
                    {
                        if (addLastVertex)
                        {
                            // Duplicate the first
                            vertices.Add(vertices.First());
                        }

                        outputSpline = SplineHelpers.CreateSpline(vertices, closed: false);
                    }
                }

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