using System.Linq;
using Unity.GraphToolkit.Editor;

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
    }
}
