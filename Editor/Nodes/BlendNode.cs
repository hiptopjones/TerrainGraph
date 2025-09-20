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
        public enum BlendMethod
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
            public BlendMethod BlendMethod;
            public bool IsFlipped;
            public HeightGrid Grid1;
            public HeightGrid Grid2;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(BlendMethod, IsFlipped, Grid1?.VersionHash, Grid2?.VersionHash);
            }
        }

        // Options
        private const string NODE_OPTION_METHOD_ID = "method_option";
        private const string NODE_OPTION_METHOD_TITLE = "Operation";

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
            context.AddOption<BlendMethod>(NODE_OPTION_METHOD_ID)
                .WithDisplayName(NODE_OPTION_METHOD_TITLE)
                .WithDefaultValue(BlendMethod.Maximum)
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

            if (!Enum.IsDefined(typeof(BlendMethod), input.BlendMethod))
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_OPTION_METHOD_TITLE} option invalid", this);
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
                GetNodeOptionByName(NODE_OPTION_METHOD_ID).TryGetValue(out temp.BlendMethod) &&
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
                var blendMethod = inputValues.BlendMethod;
                var isFlipped = inputValues.IsFlipped;
                var inputGrid1 = inputValues.Grid1;
                var inputGrid2 = inputValues.Grid2;

                var size = inputGrid1.Size;

                var outputGrid = new HeightGrid(size);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        var a = inputGrid1[x, y];
                        var b = inputGrid2[x, y];

                        switch (blendMethod)
                        {
                            case BlendMethod.Add:
                                outputGrid[x, y] = isFlipped ? b + a : a + b;
                                break;
                            case BlendMethod.Subtract:
                                outputGrid[x, y] = isFlipped ? b - a : a - b;
                                break;
                            case BlendMethod.Multiply:
                                outputGrid[x, y] = isFlipped ? b * a : a * b;
                                break;
                            case BlendMethod.Divide:
                                outputGrid[x, y] = isFlipped ? b / a : a / b;
                                break;
                            case BlendMethod.Minimum:
                                outputGrid[x, y] = isFlipped ? Mathf.Min(b, a) : Mathf.Min(a, b);
                                break;
                            case BlendMethod.Maximum:
                                outputGrid[x, y] = isFlipped ? Mathf.Max(b, a) : Mathf.Max(a, b);
                                break;
                            case BlendMethod.Average:
                                outputGrid[x, y] = isFlipped ? (b + a) / 2f : (a + b) / 2f;
                                break;
                            case BlendMethod.Compare:
                                outputGrid[x, y] = isFlipped ? Compare(b, a) : Compare(a, b);
                                break;
                            default:
                                // Validation ensures we don't get here
                                break;
                        }
                    }
                }

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

        private static float Compare(float a, float b)
        {
            if (a == b)
            {
                return 0;
            }

            return a > b ? -1 : 1.1f; // over 1 so it's green in the preview
        }
    }
}