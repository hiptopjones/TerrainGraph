using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeFirst.TerrainGraph.Editor
{
    public class ClassModel
    {
        public readonly Type ClassType;
        public readonly List<FieldModel> FieldModels = new();

        public ClassModel(Type classType)
        {
            ClassType = classType;
        }

        public FieldModel GetFieldModel(string name)
        {
            return FieldModels.SingleOrDefault(x => x.Name == name);
        }
    }
}