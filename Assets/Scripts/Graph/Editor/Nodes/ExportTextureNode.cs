using System;
using System.IO;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
public class ExportTextureNode : Node,
    IValidatableNode
{
    private int _generationId;

    private const string NODE_INPUT_GRID_ID = "grid_input";
    private const string NODE_INPUT_GRID_TITLE = "Grid";
    private const string NODE_INPUT_PATH_ID = "path_input";
    private const string NODE_INPUT_PATH_TITLE = "Path";

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        // Input
        context.AddInputPort<float[,]>(NODE_INPUT_GRID_ID)
            .WithDisplayName(NODE_INPUT_GRID_TITLE)
            .Build();

        context.AddInputPort<string>(NODE_INPUT_PATH_ID)
            .WithDisplayName(NODE_INPUT_PATH_TITLE)
            .WithDefaultValue("Assets/Textures/ExportedTexture.png")
            .Build();
    }

    public bool TryValidateNode(GraphLogger graphLogger = null)
    {
        var isValid = true;

        PortEvaluator.TryEvaluateInputPort<float[,]>(this, NODE_INPUT_GRID_ID, _generationId, out var grid);
        if (grid == null)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID_TITLE} value missing", this);
            isValid = false;
        }

        return isValid;
    }

    public void ResetNode(int generationId)
    {
        _generationId = generationId;
    }

    // TODO: Put this in an interface (IEndNode or something)
    public bool TryExecuteNode()
    {
        return TryExecuteNode(_generationId);
    }

    private bool TryExecuteNode(int generationId)
    {
        if (!TryValidateNode())
        {
            // Node validation did not pass
            return false;
        }

        if (_generationId == generationId)
        {
            // Node is already up-to-date
            return true;
        }

        ResetNode(generationId);
        
        try
        {
            PortEvaluator.TryEvaluateInputPort<float[,]>(this, NODE_INPUT_GRID_ID, _generationId, out var grid);
            PortEvaluator.TryEvaluateInputPort<string>(this, NODE_INPUT_PATH_ID, _generationId, out var exportPath);

            var texture = TextureHelpers.CreateTexture(grid);
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
