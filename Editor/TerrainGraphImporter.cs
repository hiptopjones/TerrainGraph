using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

[ScriptedImporter(1, TerrainEditorGraph.ASSET_FILE_EXTENSION)]
internal class TerrainGraphImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext context)
    {
        var graph = GraphDatabase.LoadGraphForImporter<TerrainEditorGraph>(context.assetPath);
        if (graph == null)
        {
            Debug.LogError($"Failed to load graph object: {context.assetPath}");
            return;
        }

        TryExecuteGraph(graph);
    }

    private bool TryExecuteGraph(TerrainEditorGraph graph)
    {
        var nonPreviewableNodes = graph.GetNodes().OfType<IExecutableNode>().Where(x => x is not IPreviewableNode);
        
        foreach (var node in nonPreviewableNodes)
        {
            node.TryExecuteNode();
        }

        return true;
    }
}
