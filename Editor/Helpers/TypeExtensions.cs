using System;

namespace Indiecat.TerrainGraph.Editor
{
    public static class TypeExtensions
    {
        public static bool IsGenericTypeOrSubclass(this object obj, Type genericType)
        {
            if (obj == null) return false;

            var type = obj.GetType();

            while (type != null)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType)
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }
    }
}
