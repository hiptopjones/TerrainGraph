using Unity.GraphToolkit.Editor;

namespace Indiecat.TerrainGraph.Editor
{
    public static class PortBuilderExtensions
    {
        public static IPortBuilder<T> WithRange<T>(this IPortBuilder<T> builder, string portName, PortRangeManager rangeManager, float min, float max)
        {
            rangeManager.PortRanges[portName] = (min, max);
            return builder;
        }

        public static IPortBuilder<T> WithRange<T>(this IPortBuilder<T> builder, string portName, PortRangeManager rangeManager, int min, int max)
        {
            rangeManager.PortRanges[portName] = (min, max);
            return builder;
        }
    }
}
