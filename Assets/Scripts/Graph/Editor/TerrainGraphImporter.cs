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

        //context.AddObjectToAsset(OutputTextureAssetIdName, OutputTexture);
        //context.SetMainObject(OutputTexture);
    }

    private bool TryExecuteGraph(TerrainEditorGraph graph)
    {
        var endNodes = graph.GetNodes().OfType<ExportNode>();

        foreach (var endNode in endNodes)
        {
            endNode.TryExecuteNode();
        }

        return true;
    }
}
