using System;

namespace CodeFirst.TerrainGraph.Editor
{
    [AttributeUsage(AttributeTargets.Field)]
    public class MinValueAttribute : Attribute
    {
        public readonly float Min;

        public MinValueAttribute(float min)
        {
            Min = min;
        }
    }
}
