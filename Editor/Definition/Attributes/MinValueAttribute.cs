using System;

namespace Indiecat.TerrainGraph.Editor
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class MinValueAttribute : Attribute
    {
        public readonly float Min;

        public MinValueAttribute(float min)
        {
            Min = min;
        }
    }
}
