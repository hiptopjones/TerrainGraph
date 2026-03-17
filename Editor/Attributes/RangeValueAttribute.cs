using System;

namespace CodeFirst.TerrainGraph.Editor
{
    [AttributeUsage(AttributeTargets.Field)]
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
