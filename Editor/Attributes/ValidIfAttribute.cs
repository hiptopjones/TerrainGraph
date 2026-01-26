using System;

namespace Indiecat.TerrainGraph.Editor
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class ValidIfAttribute : Attribute
    {
        public readonly string MethodName;

        public ValidIfAttribute(string predicateName)
        {
            MethodName = predicateName;
        }
    }
}
