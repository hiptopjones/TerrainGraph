using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEditor;

namespace Indiecat.TerrainGraph.Editor
{
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
            if (!IsUpdateEnabled())
            {
                // TODO: We should give the user a hint about this flag being set, but not spam them...
                return;
            }

            // Always update nodes in dependency order
            //  - Validation of one node must not look for values in an unvalidated node
            //  - Nodes providing values have always gone through validation before they are queried
            var orderedNodes = GraphHelpers.GetOrderedNodes(this);

            foreach (var node in orderedNodes)
            {
                var validatableNode = (IValidatableNode)node;
                if (validatableNode.TryValidateNode(graphLogger))
                {
                    // Do not execute without successful validation
                    var executableNode = (IExecutableNode)node;
                    executableNode.TryExecuteNode();
                }

                var previewableNode = (IPreviewableNode)node;
                previewableNode.TryUpdatePreview();
            }
        }

        private bool IsUpdateEnabled()
        {
            // Create a variable in the blackboard with the below properties to enable this check

            var variable = GetVariables().SingleOrDefault(x => x.name == "UpdateEnabled" && x.dataType == typeof(bool));
            if (variable != null)
            {
                if (variable.TryGetDefaultValue(out bool isEnabled))
                {
                    return isEnabled;
                }
            }

            return true;
        }
    }
}
