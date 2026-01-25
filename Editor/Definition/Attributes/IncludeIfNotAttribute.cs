using System;

namespace Indiecat.TerrainGraph.Editor
{
    [AttributeUsage(AttributeTargets.Field)]
    public class IncludeIfNotAttribute : IncludeIfAttribute
    {
        public IncludeIfNotAttribute(string predicateName)
            : base($"!{predicateName}")
        {
        }
    }
}
