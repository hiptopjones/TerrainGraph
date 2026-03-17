using System;
using System.Collections.Generic;
using System.Reflection;

namespace CodeFirst.TerrainGraph.Editor
{
    public static class ClassModelCache
    {
        private static readonly Dictionary<Type, ClassModel> _cache = new();

        public static ClassModel GetClassModel(string typeName) => GetClassModel(Type.GetType(typeName));

        public static ClassModel GetClassModel<T>() => GetClassModel(typeof(T));

        public static ClassModel GetClassModel(Type type)
        {
            if (!_cache.TryGetValue(type, out var classModel))
            {
                classModel = ClassModelBuilder.BuildClassModel(type);
                _cache[type] = classModel;
            }

            return classModel;
        }

        public static FieldModel GetFieldModel(FieldInfo fieldInfo)
        {
            var classModel = GetClassModel(fieldInfo.ReflectedType);
            return classModel.GetFieldModel(fieldInfo.Name);
        }
    }
}