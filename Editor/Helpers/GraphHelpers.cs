using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    public static class GraphHelpers
    {
        private static readonly Type _baseNodeType = typeof(BaseNode<>);

        public static List<INode> GetOrderedNodes(Graph graph)
        {
            return GetNodeChains(graph).SelectMany(x => x).ToList();
        }

        public static List<List<INode>> GetNodeChains(Graph graph)
        {
            var nodes = graph.GetNodes()
                .Where(x => x != null && x.IsGenericTypeOrSubclass(_baseNodeType))
                .ToList();

            return BuildChains(nodes);
        }

        private static List<List<INode>> BuildChains(List<INode> nodes)
        {
            var unresolvedInputCount = new Dictionary<INode, int>();
            var forwardGraph = new Dictionary<INode, List<INode>>();

            // Initialize dictionaries
            foreach (var node in nodes)
            {
                unresolvedInputCount[node] = 0;
                forwardGraph[node] = new List<INode>();
            }

            // Build dependency graph
            foreach (var node in nodes)
            {
                foreach (var inputPort in node.GetInputPorts())
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
                            continue;
                        }

                        // connectedNode -> node
                        forwardGraph[connectedNode].Add(node);
                        unresolvedInputCount[node]++;
                    }
                }
            }

            // Start with nodes that have no dependencies
            var readyNodes = new Queue<INode>(unresolvedInputCount.Where(p => p.Value == 0).Select(p => p.Key));

            var nodeChains = new List<List<INode>>();
            int processedCount = 0;

            while (readyNodes.Count > 0)
            {
                var startNode = readyNodes.Dequeue();
                var chain = WalkChain(startNode, forwardGraph, unresolvedInputCount, readyNodes, nodes.Count, ref processedCount);
                nodeChains.Add(chain);
            }

            return nodeChains;
        }

        private static List<INode> WalkChain(
            INode start,
            Dictionary<INode, List<INode>> forwardGraph,
            Dictionary<INode, int> unresolvedInputs,
            Queue<INode> ready,
            int totalNodeCount,
            ref int processedCount)
        {
            var chain = new List<INode>();
            var node = start;

            while (node != null)
            {
                chain.Add(node);
                processedCount++;

                if (processedCount > totalNodeCount)
                {
                    throw new Exception("Cycle detected in node graph!");
                }

                if (!forwardGraph.TryGetValue(node, out var dependents) || dependents.Count == 0)
                {
                    break;
                }

                INode nextInChain = null;

                foreach (var dependent in dependents)
                {
                    unresolvedInputs[dependent]--;

                    if (unresolvedInputs[dependent] == 0)
                    {
                        // Continue chain inline only if there is exactly one dependent
                        if (dependents.Count == 1)
                        {
                            nextInChain = dependent;
                        }
                        else
                        {
                            ready.Enqueue(dependent);
                        }
                    }
                }

                node = nextInChain;
            }

            return chain;
        }
    }
}