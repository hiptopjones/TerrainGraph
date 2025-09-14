using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

public static class PortEvaluator
{
    public static bool TryEvaluateInputPort<T>(INode node, string portId, out T value)
    {
        value = default;

        try
        {
            var port = node.GetInputPortByName(portId);

            // Sanity check, since I keep wasting time finding these mismatches
            if (typeof(T) != port.dataType)
            {
                Debug.Log($"Type mismatch on {node} input port {portId}: {typeof(T).Name} != {port.dataType}");
                return false;
            }

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
                    return evaluatableNode.TryGetOutputValue(connectedPort, out value);

                default:
                    throw new Exception($"Unhandled node type: {connectedNode?.GetType().Name}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
    }
}
