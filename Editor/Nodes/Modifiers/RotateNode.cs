using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class RotateNode : ExecutableNode<HeightGrid>
    {
        private class InputValues
        {
            public HeightGrid Grid;
            public float RotationDegrees;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(Grid?.VersionHash, RotationDegrees);
            }
        }

        // Options

        // Inputs
        private const string NODE_INPUT_GRID_ID = "grid_input";
        private const string NODE_INPUT_GRID_TITLE = "Grid";

        private const string NODE_INPUT_ROTATION_ID = "degrees_input";
        private const string NODE_INPUT_ROTATION_TITLE = "Degrees";

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
            context.AddInputPort<float>(NODE_INPUT_ROTATION_ID)
                .WithDisplayName(NODE_INPUT_ROTATION_TITLE)
                .WithDefaultValue(0)
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
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_ROTATION_ID, out temp.RotationDegrees);

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
                var rotationDegrees = inputValues.RotationDegrees;

                var size = inputGrid.Size;

                var outputGrid = new HeightGrid(size);

                var center = Vector2.one * size / 2;

                float radians = rotationDegrees * Mathf.Deg2Rad;
                float sin = Mathf.Sin(radians);
                float cos = Mathf.Cos(radians);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        var position = new Vector2(x, y) - center;

                        var sourceX = position.x * cos - position.y * sin;
                        var sourceY = position.x * sin + position.y * cos;

                        sourceX += center.x;
                        sourceY += center.y;

                        if (sourceX < 0 || sourceX > size - 1 ||
                            sourceY < 0 || sourceY > size - 1)
                        {
                            outputGrid[x, y] = 0;
                        }
                        else
                        {
                            outputGrid[x, y] = GridHelpers.SafeIndex(inputGrid, sourceX, sourceY);
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