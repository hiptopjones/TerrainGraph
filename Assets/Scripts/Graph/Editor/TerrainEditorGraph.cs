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
    private int _generationId;

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
        ValidateNodes(graphLogger);

        UpdatePreviews(_generationId++);
    }

    private void ValidateNodes(GraphLogger graphLogger)
    {
        var nodes = GetNodes().OfType<IValidatableNode>().ToList();
     
        foreach (var node in nodes)
        {
            node.TryValidateNode(graphLogger);
        }
    }

    private void UpdatePreviews(int generationId)
    {
        var nodes = GetNodes().OfType<IPreviewableNode>().ToList();

        foreach (var node in nodes)
        {
            node.UpdatePreview(generationId);
        }
    }
}
