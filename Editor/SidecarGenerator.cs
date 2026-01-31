using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    public static class SidecarGenerator
    {
        // NOTE: Not going for valid YAML, just want readable graph changes

        public static void WriteSidecar(string assetPath, TerrainEditorGraph graph)
        {
            var sidecarPath = Path.ChangeExtension(assetPath, "sidecar.yaml");
            var yaml = SerializeGraphToYaml(graph);

            if (File.Exists(sidecarPath))
            {
                var existing = File.ReadAllText(sidecarPath);
                if (existing == yaml)
                {
                    return; // Prevent import loops & dirty diffs
                }
            }

            File.WriteAllText(sidecarPath, yaml, Encoding.UTF8);
        }

        static string SerializeGraphToYaml(TerrainEditorGraph graph)
        {
            var builder = new StringBuilder(1024);

            builder.AppendLine($"version: {TerrainEditorGraph.CURRENT_VERSION}");
            builder.AppendLine("nodes:");

            var orderedNodes = GraphHelpers.GetOrderedNodes(graph);

            // Sort by ID for deterministic ordering?
            foreach (var node in orderedNodes.OrderBy(x => GetShortNodeId(x)))
            {
                GetNodeValues(node, "Options", out var options, out var optionsModel);
                GetNodeValues(node, "Inputs", out var inputs, out var inputsModel);

                builder.AppendLine($"    type: {GetNodeName(node)}");
                builder.AppendLine($"    id: {GetShortNodeId(node)}");

                var customOptionModels = optionsModel.FieldModels.Where(x => x.IsCustom);
                if (customOptionModels.Any())
                {
                    builder.AppendLine("    options:");
                    foreach (var fieldModel in customOptionModels)
                    {
                        var value = fieldModel.GetValue(options);
                        builder.AppendLine(
                            $"      {fieldModel.Name}: {FormatValue(value)}"
                        );
                    }
                }

                builder.AppendLine("    inputs:");

                if (inputs == null)
                {
                    // Inputs didn't make it through validation
                    // TODO: Read the ports directly?
                    builder.AppendLine("      invalid");
                }
                else
                {
                    foreach (var fieldModel in inputsModel.FieldModels)
                    {
                        if (fieldModel.IsCustom)
                        {
                            var port = node.GetInputPorts().FirstOrDefault(x => x.name == fieldModel.PortName);
                            if (port == null)
                            {
                                continue;
                            }

                            if (fieldModel.FieldType == typeof(HeightGrid) ||
                                fieldModel.FieldType == typeof(SplineWrapper) ||
                                fieldModel.FieldType == typeof(SplineListWrapper))
                            {
                                var connectedPort = port.firstConnectedPort;
                                var connectedNode = connectedPort.GetNode();

                                builder.AppendLine(
                                    $"      {fieldModel.Name}: {GetNodeName(connectedNode)} ({GetShortNodeId(connectedNode)})"
                                );
                            }
                            else
                            {
                                var value = fieldModel.GetValue(inputs);
                                builder.AppendLine(
                                    $"      {fieldModel.Name}: {FormatValue(value)}"
                                );
                            }
                        }
                    }
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static void GetNodeValues(INode node, string fieldName, out object classInstance, out ClassModel classModel)
        {
            var fieldInfo = node.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            classInstance = fieldInfo.GetValue(node);
            classModel = ClassModelCache.GetClassModel(fieldInfo.FieldType);
        }

        public static string GetNodeName(INode node)
        {
            return node.GetType().Name;
        }

        public static string GetShortNodeId(INode node)
        {
            var idFieldInfo = node.GetType().GetField("Id", BindingFlags.Instance | BindingFlags.Public);
            return Convert.ToString(idFieldInfo.GetValue(node)).Substring(24); // Shortened version
        }

        static string FormatFloat(float value)
        {
            // Fixed precision, culture-invariant
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        static string FormatValue(object value)
        {
            switch (value)
            {
                case null:
                    return "null";

                case float f:
                    return FormatFloat(f);

                case double d:
                    return d.ToString("0.####", CultureInfo.InvariantCulture);

                case bool b:
                    return b ? "true" : "false";

                case int or long:
                    return value.ToString();

                case string s:
                    return QuoteIfNeeded(s);

                case Vector2 v:
                    return $"[{FormatFloat(v.x)}, {FormatFloat(v.y)}]";

                case Vector3 v:
                    return $"[{FormatFloat(v.x)}, {FormatFloat(v.y)}, {FormatFloat(v.z)}]";

                case AnimationCurve c:
                    return c.ToString(); // TODO: How to show detail briefly?

                case Gradient g:
                    return g.ToString(); // TODO: How to show detail briefly?

                case Texture t:
                    return t.name;

                default:
                    // Fallback: stable string, not ToString() on Unity objects
                    return QuoteIfNeeded(value.ToString());
            }
        }

        static string QuoteIfNeeded(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "\"\"";

            // Quote only when needed to keep diffs clean
            if (s.Any(c => char.IsWhiteSpace(c) || c == ':' || c == '#'))
            {
                return $"\"{s.Replace("\"", "\\\"")}\"";
            }

            return s;
        }
    }
}