using System;
using System.IO;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
internal class ExportTextureNode : Node, IValidatedNode
{
    internal const string INPUT_PORT_GRID_ID = "grid";
    internal const string INPUT_PORT_PATH_ID = "path";

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        // Input
        context.AddInputPort<float[,]>(INPUT_PORT_GRID_ID)
            .WithDisplayName("Grid")
            .Build();

        context.AddInputPort<string>(INPUT_PORT_PATH_ID)
            .WithDisplayName("File Path")
            .WithDefaultValue("Assets/Textures/ExportedTexture.png")
            .Build();
    }

    public void ValidateNode(GraphLogger graphLogger)
    {
        // TODO
    }

    // TODO: Put this in an interface (IEndNode or something)
    public bool TryExecuteNode()
    {
        try
        {
            var heights = PortEvaluator.EvaluatePort<float[,]>(GetInputPortByName(INPUT_PORT_GRID_ID));
            var exportPath = PortEvaluator.EvaluatePort<string>(GetInputPortByName(INPUT_PORT_PATH_ID));

            if (heights == null)
            {
                return false;
            }

            var texture = TextureHelpers.CreateTexture(heights);
            var bytes = texture.EncodeToPNG();

            Directory.CreateDirectory(Path.GetDirectoryName(exportPath));
            File.WriteAllBytes(exportPath, bytes);

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
    }
}
