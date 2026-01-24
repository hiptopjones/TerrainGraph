using System;

namespace Indiecat.TerrainGraph.Editor
{
    [AttributeUsage(AttributeTargets.Field)]
    public class IgnoreIfOptionAttribute : Attribute
    {
        public readonly string OptionName;
        public readonly object Value;

        public IgnoreIfOptionAttribute(string optionName, object value)
        {
            OptionName = optionName;
            Value = value;
        }
    }
}
