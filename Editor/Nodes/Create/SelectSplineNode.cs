using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class SelectSplineNode : ExecutableNode<SplineWrapper>
    {
        private class InputValues
        {
            public SplineListWrapper SplinesWrapper;
            public int SplineIndex;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(SplinesWrapper?.VersionHash, SplineIndex);
            }
        }

        // Options

        // Input
        private const string NODE_INPUT_SPLINES_ID = "splines_input";
        private const string NODE_INPUT_SPLINES_TITLE = "Splines";

        private const string NODE_INPUT_INDEX_ID = "index_input";
        private const string NODE_INPUT_INDEX_TITLE = "Spline Index";

        // Output
        private const string NODE_OUTPUT_SPLINE_ID = "spline_output";
        private const string NODE_OUTPUT_SPLINE_TITLE = "Spline";

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
            context.AddInputPort<SplineListWrapper>(NODE_INPUT_SPLINES_ID)
                .WithDisplayName(NODE_INPUT_SPLINES_TITLE)
                .Build();
            context.AddInputPort<int>(NODE_INPUT_INDEX_ID)
                .WithDisplayName(NODE_INPUT_INDEX_TITLE)
                .WithDefaultValue(0)
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

            if (input.SplinesWrapper == null || !input.SplinesWrapper.IsValid)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SPLINES_TITLE} value missing", this);
                isValid = false;
            }

            if (input.SplineIndex < 0)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_INDEX_TITLE} value invalid: {input.SplineIndex} (valid: 0 <= n)", this);
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
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SPLINES_ID, out temp.SplinesWrapper) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_INDEX_ID, out temp.SplineIndex);

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
                var inputSplinesWrapper = inputValues.SplinesWrapper;
                var splineIndex = inputValues.SplineIndex;

                var inputSplineWrappers = inputSplinesWrapper.Splines;
                if (inputSplineWrappers.Count <= splineIndex)
                {
                    Debug.LogError($"Spline index invalid ({inputSplineWrappers.Count} splines available)");
                    return false;
                }

                var outputSpline = inputSplineWrappers[splineIndex];

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