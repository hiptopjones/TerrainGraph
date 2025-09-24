using System;
using System.IO;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ExportTextureNode : Node,
        IValidatableNode,
        IExecutableNode
    {
        private enum FileFormat
        {
            PNG,
            RAW,
        }

        private class InputValues
        {
            public FileFormat FileFormat;
            public TextureFormat TextureFormat;
            public HeightGrid Grid;
            public string ExportFilePath;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(TextureFormat, FileFormat, Grid?.VersionHash, ExportFilePath);
            }
        }

        // Options
        private const string NODE_INPUT_TEXTURE_FORMAT_ID = "texture_format_input";
        private const string NODE_INPUT_TEXTURE_FORMAT_TITLE = "Texture Format";

        private const string NODE_OPTION_FILE_FORMAT_ID = "format_input";
        private const string NODE_OPTION_FILE_FORMAT_TITLE = "File Format";

        // Inputs
        private const string NODE_INPUT_GRID_ID = "grid_input";
        private const string NODE_INPUT_GRID_TITLE = "Grid";

        private const string NODE_INPUT_PATH_ID = "path_input";
        private const string NODE_INPUT_PATH_TITLE = "Path";

        // Outputs

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<FileFormat>(NODE_OPTION_FILE_FORMAT_ID)
                .WithDisplayName(NODE_OPTION_FILE_FORMAT_TITLE)
                .WithDefaultValue(FileFormat.PNG)
                .Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            GetNodeOptionByName(NODE_OPTION_FILE_FORMAT_ID).TryGetValue(out FileFormat fileFormat);

            // Input
            context.AddInputPort<HeightGrid>(NODE_INPUT_GRID_ID)
                .WithDisplayName(NODE_INPUT_GRID_TITLE)
                .Build();

            if (fileFormat != FileFormat.RAW)
            {
                context.AddInputPort<TextureFormat>(NODE_INPUT_TEXTURE_FORMAT_ID)
                    .WithDisplayName(NODE_INPUT_TEXTURE_FORMAT_TITLE)
                    .WithDefaultValue(TextureFormat.R16)
                    .Build();
            }
            context.AddInputPort<string>(NODE_INPUT_PATH_ID)
                .WithDisplayName(NODE_INPUT_PATH_TITLE)
                .WithDefaultValue("Assets/Textures/ExportedTexture.png")
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

            if (!Enum.IsDefined(typeof(FileFormat), input.FileFormat))
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_OPTION_FILE_FORMAT_TITLE} option invalid", this);
                isValid = false;
            }

            if (input.FileFormat != FileFormat.RAW && !Enum.IsDefined(typeof(TextureFormat), input.TextureFormat))
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_TEXTURE_FORMAT_TITLE} option invalid", this);
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
                GetNodeOptionByName(NODE_OPTION_FILE_FORMAT_ID).TryGetValue(out temp.FileFormat) &&
                (temp.FileFormat == FileFormat.RAW || PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_TEXTURE_FORMAT_ID, out temp.TextureFormat)) &&
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
            if (!TryGetValidatedInputValues(out var inputValues))
            {
                // Not in valid state
                return false;
            }

            try
            {
                var fileFormat = inputValues.FileFormat;
                var textureFormat = fileFormat != FileFormat.RAW ? inputValues.TextureFormat : TextureFormat.RGB24;
                var inputGrid = inputValues.Grid;
                var exportFilePath = inputValues.ExportFilePath;

                switch (fileFormat)
                {
                    case FileFormat.PNG:
                        TryExportPng(inputGrid, textureFormat, exportFilePath);
                        break;

                    case FileFormat.RAW:
                        TryExportRaw(inputGrid, exportFilePath);
                        break;
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

        private static bool TryExportPng(HeightGrid inputGrid, TextureFormat textureFormat, string exportFilePath)
        {
            Texture2D exportTexture = null;

            try
            {
                var renderTexture = inputGrid.RenderTexture;

                if (!TextureHelpers.TryCopyRenderTextureToTexture2D(renderTexture, textureFormat, out exportTexture))
                {
                    return false;
                }

                var bytes = exportTexture.EncodeToPNG();

                Directory.CreateDirectory(Path.GetDirectoryName(exportFilePath));

                exportFilePath = Path.ChangeExtension(exportFilePath, "png");
                File.WriteAllBytes(exportFilePath, bytes);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
            finally
            {
                if (exportTexture != null)
                {
                    Object.DestroyImmediate(exportTexture);
                    exportTexture = null;
                }
            }
        }

        private static bool TryExportRaw(HeightGrid inputGrid, string exportFilePath)
        {
            var size = inputGrid.Size;

            var heights = new float[size, size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    heights[x, y] = inputGrid[x, y];
                }
            }

            if (!Raw16Writer.TryEncodeRaw16(heights, out var bytes))
            {
                return false;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(exportFilePath));

                exportFilePath = Path.ChangeExtension(exportFilePath, "raw");
                File.WriteAllBytes(exportFilePath, bytes);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }

            return true;
        }
    }
}
