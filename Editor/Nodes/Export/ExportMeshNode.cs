using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ExportMeshNode : Node,
        IValidatableNode,
        IExecutableNode
    {
        private class InputValues
        {
            public HeightGrid Grid;
            public bool IgnoreZero;
            public float HeightScale;
            public string ExportPath;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(Grid?.VersionHash, IgnoreZero, HeightScale, ExportPath);
            }
        }

        // Options

        // Inputs
        private const string NODE_INPUT_GRID_ID = "grid_input";
        private const string NODE_INPUT_GRID_TITLE = "Grid";

        private const string NODE_OPTION_ZERO_ID = "zero_option";
        private const string NODE_OPTION_ZERO_TITLE = "Ignore Zero";

        private const string NODE_INPUT_SCALE_ID = "scale_input";
        private const string NODE_INPUT_SCALE_TITLE = "Height Scale";

        private const string NODE_INPUT_PATH_ID = "path_input";
        private const string NODE_INPUT_PATH_TITLE = "Path";

        // Outputs

        // Other
        private const float DEFAULT_SCALE = 100;

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<bool>(NODE_OPTION_ZERO_ID)
                .WithDisplayName(NODE_OPTION_ZERO_TITLE)
                .WithDefaultValue(false)
                .Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input
            context.AddInputPort<HeightGrid>(NODE_INPUT_GRID_ID)
                .WithDisplayName(NODE_INPUT_GRID_TITLE)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_SCALE_ID)
                .WithDisplayName(NODE_INPUT_SCALE_TITLE)
                .WithDefaultValue(DEFAULT_SCALE)
                .Build();
            context.AddInputPort<AdaptiveLengthStringParameter>(NODE_INPUT_PATH_ID)
                .WithDisplayName(NODE_INPUT_PATH_TITLE)
                .WithDefaultValue("Assets/Models/ExportedMesh.obj")
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
                GetNodeOptionByName(NODE_OPTION_ZERO_ID).TryGetValue(out temp.IgnoreZero) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SCALE_ID, out temp.HeightScale) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_PATH_ID, out temp.ExportPath);

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

            Texture2D workingTexture = null;

            try
            {
                var inputGrid = inputValues.Grid;
                var ignoreZero = inputValues.IgnoreZero;
                var heightScale = inputValues.HeightScale;
                var exportPath = inputValues.ExportPath;

                var renderTexture = inputGrid.RenderTexture;

                if (!TextureHelpers.TryCopyRenderTextureToTexture2D(renderTexture, TextureFormat.RFloat, out workingTexture))
                {
                    return false;
                }

                var size = renderTexture.width;

                var rawHeights = new float[size * size];
                var heights = new float[size, size];

                var rawTextureData = workingTexture.GetRawTextureData<float>();
                rawTextureData.CopyTo(rawHeights);

                GridHelpers.CopyHeights(rawHeights, heights);

                MeshHelpers.ExportMesh(heights, heightScale, ignoreZero, exportPath);

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
            finally
            {
                if (workingTexture != null)
                {
                    Object.DestroyImmediate(workingTexture);
                    workingTexture = null;
                }
            }
        }
    }
}
