using System;

namespace Indiecat.TerrainGraph.Editor
{
    [AttributeUsage(AttributeTargets.Field)]
    public class IncludeIfAttribute : Attribute
    {
        public readonly string PredicateName;

        public IncludeIfAttribute(string predicateName)
        {
            PredicateName = predicateName;
        }
    }
}
