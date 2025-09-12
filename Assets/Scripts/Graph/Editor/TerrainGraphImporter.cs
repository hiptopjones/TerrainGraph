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
        // TODO
        return true;
    }
}
