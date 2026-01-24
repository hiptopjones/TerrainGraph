using System;

namespace Indiecat.TerrainGraph.Editor
{
    [AttributeUsage(AttributeTargets.Field)]
    public class DisplayNameAttribute : Attribute
    {
        public readonly string DisplayName;

        public DisplayNameAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }
}
