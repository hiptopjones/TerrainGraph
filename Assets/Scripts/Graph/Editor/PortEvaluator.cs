using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

public static class PortEvaluator
{
    private static bool verbose = false;

    public static bool TryEvaluateInputPort<T>(INode node, string portId, int generationId, out T value)
    {
        var port = node.GetInputPortByName(portId);
        if (verbose) Debug.Log($"Evaluating port {port.name} on {node} for generation {generationId}");

        if (!port.isConnected)
        {
            // If no connection exists, try to get the port's embedded value (returns type default if unavailable)
            return port.TryGetValue(out value);
        }

        var connectedPort = port.firstConnectedPort;
        var connectedNode = connectedPort.GetNode();

        switch (connectedNode)
        {
            case IConstantNode contstantNode:
                return contstantNode.TryGetValue(out value);

            case IVariableNode variableNode:
                return variableNode.variable.TryGetDefaultValue(out value);

            case IEvaluatableNode<T> evaluatableNode:
                return evaluatableNode.TryGetPortValue(connectedPort, generationId, out value);

            default:
                throw new Exception($"Unhandled node type: {connectedNode.GetType().Name}");
        }
    }
}
