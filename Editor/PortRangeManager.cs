using System.Collections.Generic;
using Unity.GraphToolkit.Editor;

namespace Indiecat.TerrainGraph.Editor
{
    public class PortRangeManager
    {
        public Dictionary<string, object> PortRanges = new();

        public void UpdateRanges(INode node)
        {
            // TODO: We should only need to run this once when the node is created

            foreach ((var key, var value) in PortRanges)
            {
                if (NodeHelpers.TryGetInputPortByName(node, key, out var port))
                {
                    if (port.dataType == typeof(RangedFloatParameter))
                    {
                        (var min, var max) = ((float, float))value;
                        if (PortEvaluator.TryEvaluateInputPort(node, key, out RangedFloatParameter rangedFloat))
                        {
                            rangedFloat.UpdateRange(min, max);
                        }
                    }
                    else if (port.dataType == typeof(RangedIntParameter))
                    {
                        (var min, var max) = ((int, int))value;
                        if (PortEvaluator.TryEvaluateInputPort(node, key, out RangedIntParameter rangedInt))
                        {
                            rangedInt.UpdateRange(min, max);
                        }
                    }
                }
            }
        }
    }
}
