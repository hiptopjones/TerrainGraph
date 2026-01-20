using System.IO;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
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

            Debug.Log($"[Import] Loaded {graph.nodeCount} nodes from {Path.GetFileNameWithoutExtension(context.assetPath)}");

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
}
