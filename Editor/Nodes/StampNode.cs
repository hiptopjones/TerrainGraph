using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class StampNode : ExecutableNode<HeightGrid>
    {
        private class InputValues
        {
            public HeightGrid Grid1;
            public HeightGrid Grid2;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(Grid1?.VersionHash, Grid2?.VersionHash);
            }
        }

        // Options
        private const string NODE_OPTION_METHOD_ID = "method_option";
        private const string NODE_OPTION_METHOD_TITLE = "Blend Method";

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
            context.AddOption<bool>(NODE_OPTION_PREVIEW_ID)
                .WithDisplayName(NODE_OPTION_PREVIEW_TITLE)
                .WithDefaultValue(false)
                .Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);

            // Input
            context.AddInputPort<HeightGrid>(NODE_INPUT_GRID1_ID)
                .WithDisplayName(NODE_INPUT_GRID1_TITLE)
                .Build();
            context.AddInputPort<HeightGrid>(NODE_INPUT_GRID2_ID)
                .WithDisplayName(NODE_INPUT_GRID2_TITLE)
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
                var inputGrid1 = inputValues.Grid1;
                var inputGrid2 = inputValues.Grid2;

                var size = inputGrid1.Size;

                var outputGrid = new HeightGrid(size);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        var height1 = inputGrid1[x, y];
                        var height2 = inputGrid2[x, y];

                        if (height2 == 0)
                        {
                            outputGrid[x, y] = height1;
                        }
                        else
                        {
                            outputGrid[x, y] = height2;
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
    }
}