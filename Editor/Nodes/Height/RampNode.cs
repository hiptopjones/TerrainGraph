using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using static Indiecat.TerrainGraph.Editor.NodeConstants;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class RampNode : ExecutableNode<HeightGrid>
    {
        private class InputValues
        {
            public RampType RampType;
            public HeightGrid Grid;
            public AnimationCurve Curve;
            public Gradient Gradient;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(RampType, Grid?.VersionHash, Curve, GradientHelpers.GetHashCode(Gradient));
            }
        }

        private enum RampType
        {
            Curve = 100,
            Gradient = 200
        }

        // Options
        private const string NODE_OPTION_TYPE_ID = "type_option";
        private const string NODE_OPTION_TYPE_TITLE = "Ramp Type";

        // Inputs
        private const string NODE_INPUT_GRID_ID = "grid_input";
        private const string NODE_INPUT_GRID_TITLE = "Grid";

        private const string NODE_INPUT_CURVE_ID = "curve_input";
        private const string NODE_INPUT_CURVE_TITLE = "Curve";

        private const string NODE_INPUT_GRADIENT_ID = "gradient_input";
        private const string NODE_INPUT_GRADIENT_TITLE = "Gradient";

        // Outputs
        private const string NODE_OUTPUT_GRID_ID = "grid_output";
        private const string NODE_OUTPUT_GRID_TITLE = "Grid";

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<RampType>(NODE_OPTION_TYPE_ID)
                .WithDisplayName(NODE_OPTION_TYPE_TITLE)
                .WithDefaultValue(RampType.Curve)
                .Build();
            context.AddOption<bool>(NODE_OPTION_PREVIEW_ID)
                .WithDisplayName(NODE_OPTION_PREVIEW_TITLE)
                .WithDefaultValue(true)
                .Build();
            context.AddOption<bool>(NODE_OPTION_DISABLE_ID)
                .WithDisplayName(NODE_OPTION_DISABLE_TITLE)
                .WithDefaultValue(false)
                .Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);
            GetNodeOptionByName(NODE_OPTION_TYPE_ID).TryGetValue<RampType>(out var rampType);

            // Input
            context.AddInputPort<HeightGrid>(NODE_INPUT_GRID_ID)
                .WithDisplayName(NODE_INPUT_GRID_TITLE)
                .Build();

            switch (rampType)
            {
                case RampType.Gradient:
                    context.AddInputPort<Gradient>(NODE_INPUT_GRADIENT_ID)
                        .WithDisplayName(NODE_INPUT_GRADIENT_TITLE)
                        .WithDefaultValue(GradientHelpers.GetDefaultGradient())
                        .Build();
                    break;

                case RampType.Curve:
                default:
                    context.AddInputPort<AnimationCurve>(NODE_INPUT_CURVE_ID)
                        .WithDisplayName(NODE_INPUT_CURVE_TITLE)
                        .WithDefaultValue(AnimationCurve.EaseInOut(0, 0, 1, 1))
                        .Build();
                    break;
            }

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
            GetNodeOptionByName(NODE_OPTION_DISABLE_ID).TryGetValue(out bool isNodeSkipped);
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

            if (!Enum.IsDefined(typeof(RampType), input.RampType))
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_OPTION_TYPE_TITLE} option invalid", this);
                isValid = false;
            }

            if (input.Grid == null || !input.Grid.IsValid)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID_TITLE} value missing", this);
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
                GetNodeOptionByName(NODE_OPTION_TYPE_ID).TryGetValue<RampType>(out temp.RampType) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID_ID, out temp.Grid) &&
                (temp.RampType != RampType.Curve || PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_CURVE_ID, out temp.Curve)) &&
                (temp.RampType != RampType.Gradient || PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRADIENT_ID, out temp.Gradient));

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
            GetNodeOptionByName(NODE_OPTION_DISABLE_ID).TryGetValue(out bool isNodeDisabled);
            if (isNodeDisabled)
            {
                return PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID_ID, out value);
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
            Texture2D rampTexture = null;

            try
            {
                var inputGrid = inputValues.Grid;

                var size = inputGrid.Size;

                var rampFunction = GetRampFunction(inputValues);
                rampTexture = TextureHelpers.GetRampTexture(size, rampFunction);

                var inputTexture = inputGrid.RenderTexture;
                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader($"Shaders/{nameof(RampNode)}", out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_InTexture", inputTexture);
                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetTexture(kernel, "_RampTexture", rampTexture);

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
                if (rampTexture != null)
                {
                    Object.DestroyImmediate(rampTexture);
                    rampTexture = null;
                }
            }
        }

        private Func<float, float> GetRampFunction(InputValues inputValues)
        {
            var curve = inputValues.Curve;
            var gradient = inputValues.Gradient;

            switch (inputValues.RampType)
            {
                case RampType.Curve:
                    return (t) => curve.Evaluate(t);

                case RampType.Gradient:
                    return (t) => gradient.Evaluate(t).grayscale;

                default:
                    Debug.LogError($"Unhandled remap type: {inputValues.RampType}");
                    return (t) => 1;
            }
        }
    }
}