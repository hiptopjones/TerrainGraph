using System;
using System.Collections.Generic;
using System.Reflection;

namespace CodeFirst.TerrainGraph.Editor
{
    public static class ClassModelBuilder
    {
        public static ClassModel BuildClassModel(Type classType)
        {
            var classModel = new ClassModel(classType);

            var bindingFlags =
                BindingFlags.Public |
                BindingFlags.Instance;

            var fieldInfos = classType.GetFields(bindingFlags);

            var fieldModels = new List<FieldModel>(fieldInfos.Length);

            foreach (var fieldInfo in fieldInfos)
            {
                var fieldModel = FieldModelBuilder.BuildFieldModel(classModel, fieldInfo);
                classModel.FieldModels.Add(fieldModel);
            }

            return classModel;
        }
    }
}