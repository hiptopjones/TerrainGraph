using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;

namespace Indiecat.TerrainGraph.Editor
{
    public static class GraphHelpers
    {
        private static Type _baseNodeType = typeof(BaseNode<>);

        public static List<INode> GetOrderedNodes(Graph graph)
        {
            var nodes = graph.GetNodes()
                .Where(x => x.IsGenericTypeOrSubclass(_baseNodeType)).ToList();

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
                        if (connectedNode == null)
                        {
                            continue;
                        }

                        if (!connectedNode.IsGenericTypeOrSubclass(_baseNodeType))
                        {
                            // Ignore variable nodes, etc.
                            continue;
                        }

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