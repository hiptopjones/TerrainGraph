using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ResizeNode : ExecutableNode<HeightGrid>
    {
        private class InputValues
        {
            public HeightGrid Grid;
            public int Size;
            public bool Zoom;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(Grid?.VersionHash, Size, Zoom);
            }
        }

        // Options

        // Inputs
        private const string NODE_INPUT_GRID_ID = "grid_input";
        private const string NODE_INPUT_GRID_TITLE = "Grid";

        private const string NODE_INPUT_SIZE_ID = "size_input";
        private const string NODE_INPUT_SIZE_TITLE = "Size";

        private const string NODE_INPUT_ZOOM_ID = "zoom_input";
        private const string NODE_INPUT_ZOOM_TITLE = "Zoom";

        // Outputs
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
            context.AddInputPort<HeightGrid>(NODE_INPUT_GRID_ID)
                .WithDisplayName(NODE_INPUT_GRID_TITLE)
                .Build();
            context.AddInputPort<int>(NODE_INPUT_SIZE_ID)
                .WithDisplayName(NODE_INPUT_SIZE_TITLE)
                .WithDefaultValue(256)
                .Build();
            context.AddInputPort<bool>(NODE_INPUT_ZOOM_ID)
                .WithDisplayName(NODE_INPUT_ZOOM_TITLE)
                .WithDefaultValue(true)
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

            if (input.Grid == null || !input.Grid.IsValid)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID_TITLE} value missing", this);
                isValid = false;
            }

            if (input.Size <= 0)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SIZE_TITLE} value invalid: {input.Size} (valid: 0 < n)", this);
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
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID_ID, out temp.Grid) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SIZE_ID, out temp.Size) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_ZOOM_ID, out temp.Zoom);

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
                var inputGrid = inputValues.Grid;
                var inputSize = inputGrid.Size;
                var outputSize = inputValues.Size;
                var zoom = inputValues.Zoom;

                HeightGrid outputGrid = null;

                if (zoom)
                {
                    outputGrid = Zoom(inputGrid, outputSize);
                }
                else
                {
                    var scalePercent = outputSize / (float)inputSize;
                    outputGrid = Resample(inputGrid, outputSize, scalePercent);
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

        public static HeightGrid Zoom(HeightGrid inputGrid, int outputSize)
        {
            var inputSize = inputGrid.Size;
        
            var outputCenter = Vector2Int.one * outputSize / 2;
            var inputCenter = Vector2Int.one * inputSize / 2;

            var outputGrid = new HeightGrid(outputSize);

            for (int y = 0; y < outputSize; y++)
            {
                for (int x = 0; x < outputSize; x++)
                {
                    var target = new Vector2Int(x, y);
                    var source = target - outputCenter + inputCenter;

                    if (source.x < 0 || source.x > inputSize - 1 ||
                        source.y < 0 || source.y > inputSize - 1)
                    {
                        outputGrid[x, y] = 0;
                    }
                    else
                    {
                        outputGrid[x, y] = inputGrid[source.x, source.y];
                    }
                }
            }

            return outputGrid;
        }

        public static HeightGrid Resample(HeightGrid inputGrid, int outputSize, float scalePercent)
        {
            var inputSize = inputGrid.Size;

            var outputCenter = Vector2Int.one * outputSize / 2;
            var inputCenter = Vector2Int.one * inputSize / 2;

            var outputGrid = new HeightGrid(outputSize);

            for (int y = 0; y < outputSize; y++)
            {
                for (int x = 0; x < outputSize; x++)
                {
                    var target = new Vector2(x, y);
                    var source = (target - outputCenter) / scalePercent + inputCenter;

                    if (source.x < 0 || source.x > inputSize - 1 ||
                        source.y < 0 || source.y > inputSize - 1)
                    {
                        outputGrid[x, y] = 0;
                    }
                    else
                    {
                        outputGrid[x, y] = GridHelpers.SafeIndex(inputGrid, source.x, source.y);
                    }
                }
            }

            return outputGrid;
        }
    }
}