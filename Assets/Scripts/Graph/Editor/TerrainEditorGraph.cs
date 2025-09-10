using System;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEditor;

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
        ValidateNodes(graphLogger);

        UpdatePreviews();
    }

    private void ValidateNodes(GraphLogger graphLogger)
    {
        var nodes = GetNodes().OfType<IValidatableNode>().ToList();
     
        foreach (var node in nodes)
        {
            node.TryValidateNode(graphLogger);
        }
    }

    private void UpdatePreviews()
    {
        var nodes = GetNodes().OfType<IPreviewableNode>().ToList();

        foreach (var node in nodes)
        {
            node.UpdatePreview();
        }
    }
}
