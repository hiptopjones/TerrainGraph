using System;

namespace Indiecat.TerrainGraph.Editor
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class ValidatorAttribute : Attribute
    {
        public readonly string MethodName;

        public ValidatorAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}
