using System;

namespace CodeFirst.TerrainGraph.Editor
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class NotNullAttribute : Attribute
    {
        public NotNullAttribute()
        {
        }
    }
}
