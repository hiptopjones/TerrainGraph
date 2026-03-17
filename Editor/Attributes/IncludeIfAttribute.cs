using System;

namespace CodeFirst.TerrainGraph.Editor
{
    [AttributeUsage(AttributeTargets.Field)]
    public class IncludeIfAttribute : Attribute
    {
        public readonly string MethodName;

        public IncludeIfAttribute(string predicateName)
        {
            MethodName = predicateName;
        }
    }
}
