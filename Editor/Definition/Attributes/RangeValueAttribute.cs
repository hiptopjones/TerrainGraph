using System;

namespace Indiecat.TerrainGraph.Editor
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class RangeValueAttribute : Attribute
    {
        public readonly float Min;
        public readonly float Max;

        public RangeValueAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }
}
