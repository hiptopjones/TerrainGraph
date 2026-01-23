using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using static Indiecat.TerrainGraph.Editor.NodeConstants;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class MaskedFillNode : ExecutableNode<HeightGrid>
    {
        private class InputValues
        {
            public HeightGrid Grid;
            public HeightGrid MaskGrid;
            public int IterationCount;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(Grid?.VersionHash, MaskGrid?.VersionHash, IterationCount);
            }
        }

        // Options

        // Inputs
        private const string NODE_INPUT_GRID_ID = "grid_input";
        private const string NODE_INPUT_GRID_TITLE = "Grid";

        private const string NODE_INPUT_MASK_ID = "mask_input";
        private const string NODE_INPUT_MASK_TITLE = "Mask";

        private const string NODE_INPUT_ITERATIONS_ID = "iterations_input";
        private const string NODE_INPUT_ITERATIONS_TITLE = "Iterations";

        // Outputs
        private const string NODE_OUTPUT_GRID_ID = "grid_output";
        private const string NODE_OUTPUT_GRID_TITLE = "Grid";

        // Other
        private const int MIN_ITERATION_COUNT = 1;
        private const int MAX_ITERATION_COUNT = 1000;
        private const int DEFAULT_ITERATION_COUNT = 100;

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
            context.AddInputPort<HeightGrid>(NODE_INPUT_GRID_ID)
                .WithDisplayName(NODE_INPUT_GRID_TITLE)
                .Build();
            context.AddInputPort<HeightGrid>(NODE_INPUT_MASK_ID)
                .WithDisplayName(NODE_INPUT_MASK_TITLE)
                .Build();
            context.AddInputPort<int>(NODE_INPUT_ITERATIONS_ID)
                .WithDisplayName(NODE_INPUT_ITERATIONS_TITLE)
                .WithDefaultValue(DEFAULT_ITERATION_COUNT)
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

            if (input.Grid == null || !input.Grid.IsValid)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID_TITLE} value missing", this);
                isValid = false;
            }

            if (input.MaskGrid == null || !input.MaskGrid.IsValid)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_MASK_TITLE} input missing", this);
                isValid = false;
            }

            if (input.IterationCount < MIN_ITERATION_COUNT || input.IterationCount > MAX_ITERATION_COUNT)
            {
                if (graphLogger != null) graphLogger.LogWarning($"{NODE_INPUT_ITERATIONS_TITLE} value invalid: {input.IterationCount} (valid: {MIN_ITERATION_COUNT} <= n <= {MAX_ITERATION_COUNT})", this);
                input.IterationCount = Mathf.Clamp(input.IterationCount, MIN_ITERATION_COUNT, MAX_ITERATION_COUNT);
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
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_MASK_ID, out temp.MaskGrid) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_ITERATIONS_ID, out temp.IterationCount);

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
            RenderTexture tempTexture1 = null;
            RenderTexture tempTexture2 = null;

            try
            {
                var inputGrid = inputValues.Grid;
                var maskGrid = inputValues.MaskGrid;
                var iterationCount = inputValues.IterationCount;

                var size = inputGrid.Size;

                var inputTexture = inputGrid.RenderTexture;
                var maskTexture = maskGrid.RenderTexture;

                // Create ping-pong textures
                tempTexture1 = TextureHelpers.CreateRenderTexture(size, RenderTextureFormat.RFloat);
                tempTexture2 = TextureHelpers.CreateRenderTexture(size, RenderTextureFormat.RFloat);

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader($"Shaders/{nameof(MaskedFillNode)}", out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                var groups = Mathf.CeilToInt(size / 8.0f);

                Graphics.Blit(inputTexture, tempTexture1);

                for (int i = 0; i < iterationCount; i++)
                {
                    shader.SetTexture(kernel, "_InTexture", tempTexture1);
                    shader.SetTexture(kernel, "_MaskTexture", maskTexture);
                    shader.SetTexture(kernel, "_OutTexture", tempTexture2);
                    shader.SetInt("_Size", size);

                    shader.Dispatch(kernel, groups, groups, 1);

                    var swapTexture = tempTexture1;
                    tempTexture1 = tempTexture2;
                    tempTexture2 = swapTexture;
                }

                Graphics.Blit(tempTexture1, outputTexture);

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
                if (tempTexture1 != null)
                {
                    tempTexture1.Release();
                    tempTexture1 = null;
                }

                if (tempTexture2 != null)
                {
                    tempTexture2.Release();
                    tempTexture2 = null;
                }
            }
        }
    }
}