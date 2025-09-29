using NUnit.Framework;
using System;
using System.Linq;
using Unity.GraphToolkit.Editor;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Windows;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ExportSplineNode : Node,
        IValidatableNode,
        IExecutableNode
    {
        private class InputValues
        {
            public bool IsFlattened;
            public SplineWrapper SplineWrapper;
            public string TargetObjectName;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(IsFlattened, SplineWrapper?.VersionHash, TargetObjectName);
            }
        }

        // Options
        private const string NODE_OPTION_FLATTEN_ID = "flatten_option";
        private const string NODE_OPTION_FLATTEN_TITLE = "Flatten";

        // Inputs
        private const string NODE_INPUT_SPLINE_ID = "spline_input";
        private const string NODE_INPUT_SPLINE_TITLE = "Spline";

        private const string NODE_INPUT_NAME_ID = "name_input";
        private const string NODE_INPUT_NAME_TITLE = "Target Object";

        // Outputs

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<bool>(NODE_OPTION_FLATTEN_ID)
                .WithDisplayName(NODE_OPTION_FLATTEN_TITLE)
                .WithDefaultValue(true)
                .Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input
            context.AddInputPort<SplineWrapper>(NODE_INPUT_SPLINE_ID)
                .WithDisplayName(NODE_INPUT_SPLINE_TITLE)
                .Build();
            context.AddInputPort<string>(NODE_INPUT_NAME_ID)
                .WithDisplayName(NODE_INPUT_NAME_TITLE)
                .WithDefaultValue("My Spline")
                .Build();
        }

        public bool TryValidateNode(GraphLogger graphLogger = null)
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

            if (string.IsNullOrEmpty(input.TargetObjectName))
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_NAME_TITLE} value missing", this);
                isValid = false;
            }
            else
            {
                var count = Object.FindObjectsByType<SplineContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Count(x => x.name == input.TargetObjectName);
                if (count == 0)
                {
                    if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_NAME_TITLE} value invalid", this);
                    isValid = false;
                }
                else if (count > 1)
                {
                    if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_NAME_TITLE} value ambiguous", this);
                    isValid = false;
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
                GetNodeOptionByName(NODE_OPTION_FLATTEN_ID).TryGetValue<bool>(out temp.IsFlattened) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SPLINE_ID, out temp.SplineWrapper) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_NAME_ID, out temp.TargetObjectName);

            if (success)
            {
                temp.VersionHash = temp.GetHashCode();

                input = temp;
                return true;
            }

            return false;
        }

        public bool TryExecuteNode()
        {
            if (!TryGetValidatedInputValues(out var inputValues))
            {
                // Not in valid state
                return false;
            }

            try
            {
                var isFlattened = inputValues.IsFlattened;
                var inputSplineWrapper = inputValues.SplineWrapper;
                var inputTargetName = inputValues.TargetObjectName;

                var inputSpline = inputSplineWrapper.Spline;

                Spline outputSpline = inputSpline;
                if (isFlattened)
                {
                    var vertices = inputSpline.Knots.Select(k => new float3(k.Position.x, 0, k.Position.z));
                    outputSpline = new Spline(vertices);
                    outputSpline.Closed = inputSpline.Closed;
                }

                var splineContainer = Object.FindObjectsByType<SplineContainer>(FindObjectsSortMode.None)
                    .Single(x => x.name == inputTargetName);

                splineContainer.Spline = outputSpline;

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
