using System;

namespace Indiecat.TerrainGraph.Editor
{
    [AttributeUsage(AttributeTargets.Field)]
    public class DefaultValueAttribute : Attribute
    {
        public readonly object Value;

        public DefaultValueAttribute(object value)
        {
            Value = value;
        }
    }
}
