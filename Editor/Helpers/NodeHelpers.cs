using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Unity.GraphToolkit.Editor;

namespace Indiecat.TerrainGraph.Editor
{
    public static class NodeHelpers
    {
        public static bool TryGetInputPortByName(INode node, string name, out IPort port)
        {
            port = node.GetInputPorts().Where(x => x.name == name).FirstOrDefault();

            return port != null;
        }

        public static bool TryGetOutputPortByName(INode node, string name, out IPort port)
        {
            port = node.GetOutputPorts().Where(x => x.name == name).FirstOrDefault();

            return port != null;
        }

        public static string GetInputPortName(string fieldName)
        {
            return $"{fieldName}Input";
        }

        public static string GetOptionName(string fieldName)
        {
            return $"{fieldName}Option";
        }

        public static string GetDisplayName(FieldInfo fieldInfo)
        {
            var attribute = fieldInfo.GetCustomAttribute<DisplayNameAttribute>();
            var displayName = attribute?.DisplayName ?? StringHelpers.TitleCaseToWords(fieldInfo.Name);

            return displayName;
        }

        public static string GetDisplayName(Type fieldType, string fieldName)
        {
            var fieldInfo = fieldType.GetField(fieldName);
            return GetDisplayName(fieldInfo);
        }
    }
}
