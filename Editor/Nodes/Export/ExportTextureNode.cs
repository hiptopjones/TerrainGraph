using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;
using static Indiecat.TerrainGraph.Editor.NodeConstants;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ExportTextureNode : Node,
        IValidatableNode,
        IExecutableNode
    {
        private class InputValues
        {
            public HeightGrid Grid;
            public string ExportFilePath;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(Grid?.VersionHash, ExportFilePath);
            }
        }

        // Options

        // Inputs
        private const string NODE_INPUT_GRID_ID = "grid_input";
        private const string NODE_INPUT_GRID_TITLE = "Grid";

        private const string NODE_INPUT_PATH_ID = "path_input";
        private const string NODE_INPUT_PATH_TITLE = "Path";

        // Outputs

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
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
            // Input
            context.AddInputPort<HeightGrid>(NODE_INPUT_GRID_ID)
                .WithDisplayName(NODE_INPUT_GRID_TITLE)
                .Build();

            context.AddInputPort<AdaptiveLengthStringParameter>(NODE_INPUT_PATH_ID)
                .WithDisplayName(NODE_INPUT_PATH_TITLE)
                .WithDefaultValue("Assets/Textures/ExportedTexture.png")
                .Build();
        }

        public bool TryValidateNode(GraphLogger graphLogger = null)
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
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_PATH_ID, out temp.ExportFilePath);

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
            GetNodeOptionByName(NODE_OPTION_DISABLE_ID).TryGetValue(out bool isNodeDisabled);
            if (isNodeDisabled)
            {
                // Execution skipped
                return true;
            }

            if (!TryGetValidatedInputValues(out var inputValues))
            {
                // Not in valid state
                return false;
            }

            try
            {
                var inputGrid = inputValues.Grid;
                var exportFilePath = inputValues.ExportFilePath;

                if (!TextureHelpers.TryExportHeightGridTexture(inputGrid, exportFilePath))
                {
                    return false;
                }

                // Ensure the editor picks up any changes
                // NOTE: Unable to invoke a refresh directly during graph asset import
                EditorApplication.delayCall = () => AssetDatabase.Refresh();

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
