using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

internal static class PortEvaluator
{
    private static bool verbose = false;

    public static T EvaluatePort<T>(IPort port)
    {
        if (verbose) Debug.Log($"Evaluating port {port.name} on {port.GetNode()}");

        if (!port.isConnected)
        {
            // If no connection exists, try to get the port's embedded value (returns type default if unavailable)
            port.TryGetValue(out T embeddedValue);
            if (verbose) Debug.Log($"Embedded: {embeddedValue}");
            return embeddedValue;
        }

        var connectedPort = port.firstConnectedPort;
        var connectedNode = connectedPort.GetNode();

        switch (connectedNode)
        {
            case IConstantNode node:
                node.TryGetValue(out T constantValue);
                if (verbose) Debug.Log($"Constant: {constantValue}");
                return constantValue;

            case IVariableNode node:
                node.variable.TryGetDefaultValue(out T variableValue);
                if (verbose) Debug.Log($"Variable: {variableValue}");
                return variableValue;

            case IEvaluatedNode<T> node:
                node.TryGetPortValue(connectedPort, out T evaluatedValue);
                if (verbose) Debug.Log($"Evaluated: {evaluatedValue}");
                return evaluatedValue;

            default:
                throw new Exception($"Unhandled node type: {connectedNode.GetType().Name}");
        }
    }
}
