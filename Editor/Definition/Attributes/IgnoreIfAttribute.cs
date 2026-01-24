using System;

namespace Indiecat.TerrainGraph.Editor
{
    [AttributeUsage(AttributeTargets.Field)]
    public class IgnoreIfAttribute : Attribute
    {
        public readonly string PredicateName;

        public IgnoreIfAttribute(string predicateName)
        {
            PredicateName = predicateName;
        }
    }
}
