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
            var orderedNodes = GetOrderedNodes();

            foreach (var node in orderedNodes.OfType<IValidatableNode>())
            {
                node.TryValidateNode(graphLogger);
            }

            foreach (var node in orderedNodes.OfType<IPreviewableNode>())
            {
                node.TryUpdatePreview();
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

        private List<INode> GetOrderedNodes()
        {
            var nodes = GetNodes().ToList();
            var orderedNodes = TopologicalSort(nodes);

            return orderedNodes;
        }

        public static List<INode> TopologicalSort(List<INode> nodes)
        {
            // Count dependencies (in-degree)
            var inDegree = new Dictionary<INode, int>();
            var graph = new Dictionary<INode, List<INode>>();

            foreach (var node in nodes)
            {
                if (node == null)
                {
                    continue;
                }

                inDegree[node] = 0;
                graph[node] = new List<INode>();
            }

            // Build graph
            foreach (var node in nodes)
            {
                if (node == null)
                {
                    continue;
                }

                var inputPorts = node.GetInputPorts();
                foreach (var inputPort in inputPorts)
                {
                    var connectedPorts = new List<IPort>();
                    inputPort.GetConnectedPorts(connectedPorts);

                    foreach (var connectedPort in connectedPorts)
                    {
                        var connectedNode = connectedPort.GetNode();

                        graph[connectedNode].Add(node);
                        inDegree[node]++;
                    }
                }
            }

            // Start with nodes that have no dependencies
            var queue = new Queue<INode>(inDegree.Where(p => p.Value == 0).Select(p => p.Key));
            var result = new List<INode>();

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                result.Add(current);

                foreach (var dependent in graph[current])
                {
                    inDegree[dependent]--;
                    if (inDegree[dependent] == 0)
                    {
                        queue.Enqueue(dependent);
                    }
                }

                // Optional safety check (should never happen if no cycles)
                if (result.Count > nodes.Count)
                {
                    throw new System.Exception("Cycle detected in node graph!");
                }
            }

            return result;
        }
    }
}
