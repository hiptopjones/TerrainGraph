using System;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Splines;
using static Indiecat.TerrainGraph.Editor.NodeConstants;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ImportSplineNode : ExecutableNode<SplineWrapper>
    {
        private class InputValues
        {
            public string TargetObjectName;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(TargetObjectName);
            }
        }

        // Inputs
        private const string NODE_INPUT_NAME_ID = "name_input";
        private const string NODE_INPUT_NAME_TITLE = "Target Object";

        // Outputs
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
            context.AddInputPort<AdaptiveLengthStringParameter>(NODE_INPUT_NAME_ID)
                .WithDisplayName(NODE_INPUT_NAME_TITLE)
                .WithDefaultValue("My Spline")
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

            if (string.IsNullOrEmpty(input.TargetObjectName))
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_NAME_TITLE} value missing", this);
                isValid = false;
            }
            else
            {
                var splineContainers = Object.FindObjectsByType<SplineContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                var namedSplineContainerCount = splineContainers.Count(x => x.name == input.TargetObjectName);
                if (namedSplineContainerCount == 0)
                {
                    if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_NAME_TITLE} value invalid", this);
                    isValid = false;
                }
                else if (namedSplineContainerCount > 1)
                {
                    if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_NAME_TITLE} value ambiguous", this);
                    isValid = false;
                }
                else
                {
                    var splineContainer = splineContainers.First();
                    if (splineContainer.Spline == null)
                    {
                        if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_NAME_TITLE} missing spline", this);
                        isValid = false;
                    }
                }
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
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_NAME_ID, out temp.TargetObjectName);

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
                var inputTargetName = inputValues.TargetObjectName;

                var splineContainer = Object.FindObjectsByType<SplineContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Single(x => x.name == inputTargetName);

                var outputSpline = splineContainer.Spline;

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
