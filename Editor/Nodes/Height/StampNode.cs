using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class StampNode : ExecutableNode<HeightGrid>
    {
        private enum EasingType
        {
            Constant = 100,
            Linear = 200,
            Cubic = 300,
            SmoothStep = 400,
        }

        private class InputValues
        {
            public EasingType EasingType;
            public HeightGrid Grid;
            public HeightGrid StampGrid;
            public HeightGrid MaskGrid;
            public int Radius;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(EasingType, Grid?.VersionHash, StampGrid?.VersionHash, MaskGrid?.VersionHash, Radius);
            }
        }

        // Options
        private const string NODE_OPTION_TYPE_ID = "type_option";
        private const string NODE_OPTION_TYPE_TITLE = "Easing Type";

        // Inputs
        private const string NODE_INPUT_GRID_ID = "grid_input";
        private const string NODE_INPUT_GRID_TITLE = "Grid";

        private const string NODE_INPUT_STAMP_ID = "stamp_input";
        private const string NODE_INPUT_STAMP_TITLE = "Stamp";

        private const string NODE_INPUT_MASK_ID = "mask_input";
        private const string NODE_INPUT_MASK_TITLE = "Mask";

        private const string NODE_INPUT_RADIUS_ID = "radius_input";
        private const string NODE_INPUT_RADIUS_TITLE = "Radius";

        // Outputs
        private const string NODE_OUTPUT_GRID_ID = "grid_output";
        private const string NODE_OUTPUT_GRID_TITLE = "Grid";

        // Other
        private const int DEFAULT_RADIUS = 10;

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<EasingType>(NODE_OPTION_TYPE_ID)
                .WithDisplayName(NODE_OPTION_TYPE_TITLE)
                .WithDefaultValue(EasingType.SmoothStep)
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

            // Input
            context.AddInputPort<HeightGrid>(NODE_INPUT_GRID_ID)
                .WithDisplayName(NODE_INPUT_GRID_TITLE)
                .Build();
            context.AddInputPort<HeightGrid>(NODE_INPUT_STAMP_ID)
                .WithDisplayName(NODE_INPUT_STAMP_TITLE)
                .Build();
            context.AddInputPort<HeightGrid>(NODE_INPUT_MASK_ID)
                .WithDisplayName(NODE_INPUT_MASK_TITLE)
                .Build();
            context.AddInputPort<int>(NODE_INPUT_RADIUS_ID)
                .WithDisplayName(NODE_INPUT_RADIUS_TITLE)
                .WithDefaultValue(DEFAULT_RADIUS)
                .Build();

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

            if (!Enum.IsDefined(typeof(EasingType), input.EasingType))
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_OPTION_TYPE_TITLE} option invalid", this);
                isValid = false;
            }

            if (input.Grid == null || !input.Grid.IsValid)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID_TITLE} input missing", this);
                isValid = false;
            }

            if (input.StampGrid == null || !input.StampGrid.IsValid)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_STAMP_TITLE} input missing", this);
                isValid = false;
            }

            if (input.MaskGrid == null || !input.MaskGrid.IsValid)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_MASK_TITLE} input missing", this);
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
                GetNodeOptionByName(NODE_OPTION_TYPE_ID).TryGetValue(out temp.EasingType) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID_ID, out temp.Grid) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_STAMP_ID, out temp.StampGrid) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_MASK_ID, out temp.MaskGrid) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_RADIUS_ID, out temp.Radius);

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
            try
            {
                var easingType = inputValues.EasingType;
                var inputGrid = inputValues.Grid;
                var stampGrid = inputValues.StampGrid;
                var maskGrid = inputValues.MaskGrid;
                var radius = inputValues.Radius;

                var size = inputGrid.Size;

                var keywordBuilder = new KeywordBuilder();
                keywordBuilder.AddKeyword($"EASING_{easingType.ToString().ToUpper()}");

                var inputTexture = inputGrid.RenderTexture;
                var stampTexture = stampGrid.RenderTexture;
                var maskTexture = maskGrid.RenderTexture;
                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader($"Shaders/{nameof(StampNode)}", out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_InTexture", inputTexture);
                shader.SetTexture(kernel, "_StampTexture", stampTexture);
                shader.SetTexture(kernel, "_MaskTexture", maskTexture);
                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetInt("_Radius", radius);
                shader.SetInt("_Size", size);

                shader.shaderKeywords = keywordBuilder.GetKeywords();

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
        }
    }
}