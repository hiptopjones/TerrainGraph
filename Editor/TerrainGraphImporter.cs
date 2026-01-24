using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [ScriptedImporter(1, TerrainEditorGraph.ASSET_FILE_EXTENSION)]
    internal class TerrainGraphImporter : ScriptedImporter
    {
        [SerializeField] private int _version = 2;

        private const int CURRENT_VERSION = 2;

        public override void OnImportAsset(AssetImportContext context)
        {
            if (_version < CURRENT_VERSION)
            {
                UpdateAssetFile(context.assetPath);

                _version = CURRENT_VERSION;

                // IMPORTANT: mark importer dirty so Unity saves the meta
                EditorUtility.SetDirty(this);

                // Delay reimport to avoid recursion
                EditorApplication.delayCall += () => AssetDatabase.ImportAsset(context.assetPath);

                return;
            }

            var graph = GraphDatabase.LoadGraphForImporter<TerrainEditorGraph>(context.assetPath);
            if (graph == null)
            {
                Debug.LogError($"Failed to load graph object: {context.assetPath}");
                return;
            }

            Debug.Log($"[Import] Loaded {graph.nodeCount} nodes from {Path.GetFileNameWithoutExtension(context.assetPath)}");

            TryExecuteGraph(graph);
        }

        private bool TryExecuteGraph(TerrainEditorGraph graph)
        {
            // Always update nodes in dependency order
            //  - Validation of one node must not look for values in an unvalidated node
            //  - Nodes providing values have always gone through validation before they are queried
            var orderedNodes = GraphHelpers.GetOrderedNodes(graph);

            foreach (var node in orderedNodes.OfType<IValidatableNode>())
            {
                node.TryValidateNode(null);
            }

            var exportableNodes = orderedNodes.OfType<IExportableNode>();
            foreach (var node in exportableNodes)
            {
                node.TryExportNode();
            }

            return true;
        }

        private void UpdateAssetFile(string assetFilePath)
        {
            var replacements = LoadReplacementData();

            var lines = File.ReadAllLines(assetFilePath);

            ReplaceKeys(lines, replacements);

            File.WriteAllLines(assetFilePath, lines);
        }

        private Dictionary<string, Dictionary<string, string>> LoadReplacementData()
        {
            var replacements = new Dictionary<string, Dictionary<string, string>>();

            var lines = File.ReadAllLines(@"Assets\InputValues_Mappings.csv");

            var headers = lines[0].Split(',').Select(x => x.Trim()).ToList();
            var classIndex = headers.IndexOf("OuterClassName");
            var originalIndex = headers.IndexOf("Original");
            var updatedIndex = headers.IndexOf("Updated");

            foreach (var line in lines.Skip(1))
            {
                var fields = line.Split(',').Select(x => x.Trim()).ToList();
                var className = fields[classIndex];
                var originalName = fields[originalIndex];
                var updatedName = fields[updatedIndex];

                if (!replacements.TryGetValue(className, out var classReplacements))
                {
                    classReplacements = new Dictionary<string, string>();
                    replacements[className] = classReplacements;
                }

                classReplacements[originalName] = updatedName;
            }

            return replacements;
        }

        private void ReplaceKeys(string[] lines, Dictionary<string, Dictionary<string, string>> replacements)
        {
            var globalReplacements = replacements["__global"];
            var flattenedReplacements = replacements
                .SelectMany(o => o.Value)
                .GroupBy(p => p.Key)
                .ToDictionary(g => g.Key, g => g.Last().Value);

            var idToClassName = new Dictionary<string, string>();

            // First pass - map id to class name
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // - rid: 6822668016261005868
                var id = GetId(line, isCollection: true);
                if (id != null)
                {
                    var className = GetClassName(lines[i + 1]);
                    if (className != null)
                    {
                        idToClassName[id] = className;
                    }
                }
            }

            // Second pass - find and replace the keys for each node
            for (int i = 0; i < lines.Length; i++)
            {
                //  m_InputConstantsById:
                //    m_KeyList:
                //      - __option_preview_option
                //      - grid_input
                //      - iterations_input
                //  ...
                //  m_Node:
                //    - rid: 1234
                var inputConstantsPattern = @"^\s+m_InputConstantsById:$";
                if (Regex.IsMatch(lines[i], inputConstantsPattern))
                {
                    var keyListPattern = @"^\s+m_KeyList:$";
                    if (Regex.IsMatch(lines[i + 1], keyListPattern))
                    {
                        var blockIndex = i + 2;
                        var blockLength = GetKeyBlockLength(lines, blockIndex);

                        var nodeId = GetNextNodeId(lines, blockIndex + blockLength);
                        var className = idToClassName[nodeId];
                        if (replacements.ContainsKey(className))
                        {
                            ReplaceNodeKeys(lines, blockIndex, blockLength, replacements[className]);
                        }
                        else
                        {
                            Debug.Log($"no replacements found for {className}");
                        }
                    }
                }
            }

            // Third pass - find and replace global keys and wire nodes
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                var key = GetKey(line) ?? GetWireKey(line);
                if (key != null)
                {
                    // First try global replacements
                    if (globalReplacements.ContainsKey(key))
                    {
                        line = line.Replace(key, globalReplacements[key]);
                    }
                    else
                    {
                        // Then try everything else (for wire nodes)
                        if (flattenedReplacements.ContainsKey(key))
                        {
                            line = line.Replace(key, flattenedReplacements[key]);
                        }
                    }
                }

                lines[i] = line;
            }
        }

        private void ReplaceNodeKeys(string[] lines, int blockIndex, int blockLength, Dictionary<string, string> replacements)
        {
            for (int i = blockIndex; i < blockIndex + blockLength; i++)
            {
                var line = lines[i];

                var key = GetKey(line);
                if (replacements.ContainsKey(key)) // globals are not in here
                {
                    line = line.Replace(key, replacements[key]);
                }

                lines[i] = line;
            }
        }

        private int GetKeyBlockLength(string[] lines, int startIndex)
        {
            var i = startIndex;

            while (true)
            {
                var key = GetKey(lines[i]);
                if (key == null)
                {
                    return i - startIndex;
                }

                i++;
            }
        }

        private string GetId(string line, bool isCollection)
        {
            // - rid: 6822668016261005868
            // rid: 6822668016261005868
            var idPattern = isCollection ? @"^\s+-\srid: (?<Id>\d+)$" : @"^\s+rid: (?<Id>\d+)$";

            var idMatch = Regex.Match(line, idPattern);
            if (idMatch.Success)
            {
                return idMatch.Groups["Id"].Value;
            }

            return null;
        }

        private string GetKey(string line)
        {
            // - grid_input
            var keyPattern = @"^\s+-\s(?<Key>\w+)$";

            var keyMatch = Regex.Match(line, keyPattern);
            if (keyMatch.Success)
            {
                return keyMatch.Groups["Key"].Value;
            }

            return null;
        }

        private string GetWireKey(string line)
        {
            // m_UniqueId: grid_input
            var keyPattern = @"^\s+m_UniqueId:\s(?<Key>\w+)$";

            var keyMatch = Regex.Match(line, keyPattern);
            if (keyMatch.Success)
            {
                return keyMatch.Groups["Key"].Value;
            }

            return null;
        }

        private string GetClassName(string line)
        {
            // type: {class: ArithmeticNode, ns: Indiecat.TerrainGraph.Editor, asm: Indiecat.TerrainGraph.Editor}
            var classPattern = @"\s+type: {class: (?<Class>\w+), ns: Indiecat"; // Include namespace to scope matches

            var classMatch = Regex.Match(line, classPattern);
            if (classMatch.Success)
            {
                return classMatch.Groups["Class"].Value;
            }

            return null;
        }

        private string GetNextNodeId(string[] lines, int startIndex)
        {
            var i = startIndex;

            while (true)
            {
                var nodePattern = @"^\s+m_Node:$";
                if (Regex.IsMatch(lines[i], nodePattern))
                {
                    string id = GetId(lines[i + 1], isCollection: false);
                    return id;
                }

                i++;
            }
        }
    }
}
