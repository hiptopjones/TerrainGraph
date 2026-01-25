using System;

namespace Indiecat.TerrainGraph.Editor
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class ValidIfAttribute : Attribute
    {
        public readonly string PredicateName;

        public ValidIfAttribute(string predicateName)
        {
            PredicateName = predicateName;
        }
    }
}
