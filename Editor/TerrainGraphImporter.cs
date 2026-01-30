using System.IO;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [ScriptedImporter(1, TerrainEditorGraph.ASSET_FILE_EXTENSION)]
    internal class TerrainGraphImporter : ScriptedImporter
    {
        // Ensure the upgrade always runs the first time
        [SerializeField] private int _version = 1;

        private const int CURRENT_VERSION = 2;

        public override void OnImportAsset(AssetImportContext context)
        {
            if (_version < CURRENT_VERSION)
            {
                VersionUpgrader.UpdateAssetFile(context.assetPath);

                _version = CURRENT_VERSION;

                // Mark importer dirty so Unity saves the meta
                EditorUtility.SetDirty(this);

                // Delay reimport to avoid recursion
                EditorApplication.delayCall += () => AssetDatabase.ImportAsset(context.assetPath);

                return;
            }

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
            // Always update nodes in dependency order
            //  - Validation of one node must not look for values in an unvalidated node
            //  - Nodes providing values have always gone through validation before they are queried
            var orderedNodes = GraphHelpers.GetOrderedNodes(graph);

            foreach (var node in orderedNodes)
            {
                var validatableNode = (IValidatableNode)node;
                if (validatableNode.TryValidateNode(null))
                {
                    // Do not execute without successful validation
                    var executableNode = (IExecutableNode)node;
                    if (executableNode.TryExecuteNode())
                    {
                        var exportableNode = node as IExportableNode;
                        if (exportableNode != null)
                        {
                            exportableNode.TryExportNode();
                        }
                    }
                }
            }

            return true;
        }
    }
}
