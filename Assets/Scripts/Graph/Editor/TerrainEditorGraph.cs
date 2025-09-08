using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

[Graph(ASSET_FILE_EXTENSION)]
[Serializable]
public class TerrainEditorGraph : Graph
{
    // This file extension is used by Unity to select the right importer, so it must be unique.
    internal const string ASSET_FILE_EXTENSION = "trgraph";

    internal const string DEFAULT_ASSET_NAME = "Terrain Graph";

    [MenuItem("Assets/Create/Terrain Graph")]
    static void CreateAssetFile()
    {
        GraphDatabase.PromptInProjectBrowserToCreateNewAsset<TerrainEditorGraph>(DEFAULT_ASSET_NAME);
    }

    public override void OnGraphChanged(GraphLogger graphLogger)
    {
        //Debug.Log("Graph changed");

        ValidateNodes(graphLogger);
        UpdatePreviews();
    }
    private void ValidateNodes(GraphLogger graphLogger)
    {
        var nodes = GetValidatedNodes();
     
        foreach (var node in nodes)
        {
            var validatedNode = node as IValidatedNode;
            validatedNode.ValidateNode(graphLogger);
        }
    }

    private void UpdatePreviews()
    {
        var nodes = GetEvaluatedNodes();

        // Clear all the node caches
        foreach (var node in nodes)
        {
            var evaluatedNode = node as IEvaluatedNode<float[,]>;
            evaluatedNode.ResetNode();
        }

        // Generate node previews
        foreach (var node in nodes)
        {
            var evaluatedNode = node as IEvaluatedNode<float[,]>;

            if (TryGetInputPortByName(node, "preview", out var previewPort))
            {
                if (previewPort.TryGetValue(out PreviewImage previewImage))
                {
                    IPort outputPort = null;

                    if (TryGetOutputPortByName(node, "grid", out outputPort))
                    {
                        if (evaluatedNode.TryGetPortValue(outputPort, out var grid))
                        {
                            if (grid == null)
                            {
                                continue;
                            }

                            if (previewImage.Texture == null)
                            {
                                previewImage.Texture = TextureHelpers.CreateTexture(grid);
                            }
                            else
                            {
                                TextureHelpers.UpdateTexture(grid, previewImage.Texture);
                            }
                        }
                    }
                }
            }
        }
    }

    private List<INode> GetValidatedNodes()
    {
        return GetNodes().Where(x => x is IValidatedNode).ToList();
    }

    private List<INode> GetEvaluatedNodes()
    {
        return GetNodes().Where(x => x is IEvaluatedNode<float[,]>).ToList();
    }

    private bool TryGetInputPortByName(INode node, string name, out IPort port)
    {
        port = node.GetInputPorts().Where(x => x.name == name).FirstOrDefault();

        return port != null;
    }

    private bool TryGetOutputPortByName(INode node, string name, out IPort port)
    {
        port = node.GetOutputPorts().Where(x => x.name == name).FirstOrDefault();

        return port != null;
    }
}
