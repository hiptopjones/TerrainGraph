using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    public static class PortEvaluator
    {
        public static bool TryEvaluateInputPort<T>(INode node, string portId, out T value)
        {
            value = default;

            try
            {
                var port = node.GetInputPortByName(portId);

                var fromType = port.DataType;
                var toType = typeof(T);

                // Sanity check, since I keep wasting time finding type mismatches on ports
                if (fromType != toType)
                {
                    Debug.Log($"Type mismatch on {node} input port {portId}: {typeof(T).Name} != {port.DataType}");
                    return false;
                }

                // Types check out, so forward the request
                return TryEvaluateInputPortInternal(node, port, out value);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        private static bool TryEvaluateInputPortInternal<T>(INode node, IPort port, out T value)
        {
            value = default;

            try
            {
                if (!port.IsConnected)
                {
                    // If no connection exists, try to get the port's embedded value (returns type default if unavailable)
                    return port.TryGetValue(out value);
                }

                var connectedPort = port.FirstConnectedPort;
                var connectedNode = connectedPort.GetNode();
                if (connectedNode == null)
                {
                    Debug.Log($"Missing node on {node} input port {port.Name}: check for orphaned portals");
                    return false;
                }

                switch (connectedNode)
                {
                    case IConstantNode constantNode:
                        return constantNode.TryGetValue(out value);

                    case IVariableNode variableNode:
                        return variableNode.Variable.TryGetDefaultValue(out value);

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
}