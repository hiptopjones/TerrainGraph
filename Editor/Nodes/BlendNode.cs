using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class BlendNode : ExecutableNode<HeightGrid>
    {
        public enum BlendOperator
        {
            Add = 100,
            Subtract = 200,
            Multiply = 300,
            Divide = 400,
            Minimum = 500,
            Maximum = 600,
            Average = 700,
            Compare = 1000,
        }

        private class InputValues
        {
            public BlendOperator BlendOperator;
            public bool IsFlipped;
            public HeightGrid Grid1;
            public HeightGrid Grid2;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(BlendOperator, IsFlipped, Grid1?.VersionHash, Grid2?.VersionHash);
            }
        }

        // Options
        private const string NODE_OPTION_OPERATOR_ID = "operator_option";
        private const string NODE_OPTION_OPERATOR_TITLE = "Operation";

        private const string NODE_OPTION_FLIP_ID = "flipped_option";
        private const string NODE_OPTION_FLIP_TITLE = "Flip Inputs";

        // Input
        private const string NODE_INPUT_GRID1_ID = "grid1_input";
        private const string NODE_INPUT_GRID1_TITLE = "Grid 1";

        private const string NODE_INPUT_GRID2_ID = "grid2_input";
        private const string NODE_INPUT_GRID2_TITLE = "Grid 2";

        // Output
        private const string NODE_OUTPUT_GRID_ID = "grid_output";
        private const string NODE_OUTPUT_GRID_TITLE = "Grid";

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<BlendOperator>(NODE_OPTION_OPERATOR_ID)
                .WithDisplayName(NODE_OPTION_OPERATOR_TITLE)
                .WithDefaultValue(BlendOperator.Maximum)
                .Build();
            context.AddOption<bool>(NODE_OPTION_FLIP_ID)
                .WithDisplayName(NODE_OPTION_FLIP_TITLE)
                .WithDefaultValue(false)
                .Build();
            context.AddOption<bool>(NODE_OPTION_PREVIEW_ID)
                .WithDisplayName(NODE_OPTION_PREVIEW_TITLE)
                .WithDefaultValue(false)
                .Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);
            GetNodeOptionByName(NODE_OPTION_FLIP_ID).TryGetValue<bool>(out var isFlipped);

            // Input
            var actions = new List<Action>
            {
                () => context.AddInputPort<HeightGrid>(NODE_INPUT_GRID1_ID)
                    .WithDisplayName(NODE_INPUT_GRID1_TITLE)
                    .Build(),
                () => context.AddInputPort<HeightGrid>(NODE_INPUT_GRID2_ID)
                    .WithDisplayName(NODE_INPUT_GRID2_TITLE)
                    .Build(),
            };

            // All this to avoid duplicating the port definitions
            actions = isFlipped ? actions.AsEnumerable().Reverse().ToList() : actions;
            foreach (var action in actions)
            {
                action.Invoke();
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

            if (!Enum.IsDefined(typeof(BlendOperator), input.BlendOperator))
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_OPTION_OPERATOR_TITLE} option invalid", this);
                isValid = false;
            }

            if (input.Grid1 == null || !input.Grid1.IsValid)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID1_TITLE} value missing", this);
                isValid = false;
            }

            if (input.Grid2 == null || !input.Grid2.IsValid)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID2_TITLE} value missing", this);
                isValid = false;
            }

            if (isValid && input.Grid1.Values.Length != input.Grid2.Values.Length)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID1_TITLE} and {NODE_INPUT_GRID2_TITLE} size mismatch", this);
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
                GetNodeOptionByName(NODE_OPTION_OPERATOR_ID).TryGetValue(out temp.BlendOperator) &&
                GetNodeOptionByName(NODE_OPTION_FLIP_ID).TryGetValue(out temp.IsFlipped) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID1_ID, out temp.Grid1) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID2_ID, out temp.Grid2);

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
                var blendOperator = inputValues.BlendOperator;
                var isFlipped = inputValues.IsFlipped;
                var inputGrid1 = inputValues.Grid1;
                var inputGrid2 = inputValues.Grid2;

                var size = inputGrid1.Size;

                var keywordBuilder = new KeywordBuilder();
                keywordBuilder.AddKeyword($"OP_{blendOperator.ToString().ToUpper()}");
                keywordBuilder.AddKeyword(isFlipped ? "ARGS_FLIPPED" : "ARGS_NORMAL");

                Texture inputTexture1 = inputGrid1.RenderTexture;
                Texture inputTexture2 = inputGrid2.RenderTexture;
                RenderTexture outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader("Shaders/BlendNode", out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_InTexture1", inputTexture1);
                shader.SetTexture(kernel, "_InTexture2", inputTexture2);
                shader.SetTexture(kernel, "_OutTexture", outputTexture);

                shader.shaderKeywords = keywordBuilder.GetKeywords();

                int groups = Mathf.CeilToInt(size / 8.0f);
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