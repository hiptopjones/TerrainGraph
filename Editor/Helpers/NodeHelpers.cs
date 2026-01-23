using System;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using static Indiecat.TerrainGraph.Editor.NodeConstants;

namespace Indiecat.TerrainGraph.Editor
{
    public static class NodeHelpers
    {
        public static bool TryGetInputPortByName(INode node, string name, out IPort port)
        {
            port = node.GetInputPorts().Where(x => x.name == name).FirstOrDefault();

            return port != null;
        }

        public static bool TryGetOutputPortByName(INode node, string name, out IPort port)
        {
            port = node.GetOutputPorts().Where(x => x.name == name).FirstOrDefault();

            return port != null;
        }

        public static bool TrySetWarningBanner(INode node, string text)
        {
            try
            {
                // TODO: Can we get the object once, instead of on every update?
                var warningOption = ((Node)node).GetNodeOptionByName(NODE_OPTION_WARNING_ID);
                if (warningOption == null)
                {
                    Debug.Log("Unable to get the warning option");
                    return false;
                }

                if (!warningOption.TryGetValue(out WarningBanner warningBanner))
                {
                    // Unable to get preview port value, so cannot display anything
                    Debug.LogError("Unable to get the warning banner");
                    return false;
                }

                if (warningBanner == null)
                {
                    Debug.Log("Warning banner is null");
                    return false;
                }

                warningBanner.UpdateProperties(text);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }
    }
}
