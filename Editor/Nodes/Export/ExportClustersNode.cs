using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ExportClustersNode : Node,
        IValidatableNode,
        IExecutableNode
    {
        private class InputValues
        {
            public HeightGrid Grid;
            public float HeightScale;
            public string ExportPath;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(Grid?.VersionHash, HeightScale, ExportPath);
            }
        }

        // Options

        // Inputs
        private const string NODE_INPUT_GRID_ID = "grid_input";
        private const string NODE_INPUT_GRID_TITLE = "Grid";

        private const string NODE_INPUT_SCALE_ID = "scale_input";
        private const string NODE_INPUT_SCALE_TITLE = "Height Scale";

        private const string NODE_INPUT_PATH_ID = "path_input";
        private const string NODE_INPUT_PATH_TITLE = "Path";

        // Outputs

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input
            context.AddInputPort<HeightGrid>(NODE_INPUT_GRID_ID)
                .WithDisplayName(NODE_INPUT_GRID_TITLE)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_SCALE_ID)
                .WithDisplayName(NODE_INPUT_SCALE_TITLE)
                .WithDefaultValue(100)
                .Build();
            context.AddInputPort<string>(NODE_INPUT_PATH_ID)
                .WithDisplayName(NODE_INPUT_PATH_TITLE)
                .WithDefaultValue("Assets/Models/ExportedMesh.obj")
                .Build();
        }


        public bool TryValidateNode(GraphLogger graphLogger = null)
        {
            return TryGetValidatedInputValues(out _, graphLogger);
        }

        private bool TryGetValidatedInputValues(out InputValues validatedInput, GraphLogger graphLogger = null)
        {
            validatedInput = null;

            if (!TryGetInputValues(out var input))
            {
                if (graphLogger != null) graphLogger.LogError("Upstream failure", this);
                return false;
            }

            var isValid = true;

            if (input.Grid == null || !input.Grid.IsValid)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID_TITLE} value missing", this);
                isValid = false;
            }

            if (isValid)
            {
                validatedInput = input;
            }

            return isValid;
        }

        private bool TryGetInputValues(out InputValues input)
        {
            input = null;

            var temp = new InputValues();
            var success =
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID_ID, out temp.Grid) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SCALE_ID, out temp.HeightScale) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_PATH_ID, out temp.ExportPath);

            if (success)
            {
                temp.VersionHash = temp.GetHashCode();

                input = temp;
                return true;
            }

            return false;
        }

        public bool TryExecuteNode()
        {
            if (!TryGetValidatedInputValues(out var inputValues))
            {
                // Not in valid state
                return false;
            }

            Texture2D workingTexture = null;

            try
            {
                var inputGrid = inputValues.Grid;
                var heightScale = inputValues.HeightScale;
                var exportPath = inputValues.ExportPath;

                var renderTexture = inputGrid.RenderTexture;

                if (!TextureHelpers.TryCopyRenderTextureToTexture2D(renderTexture, TextureFormat.RFloat, out workingTexture))
                {
                    return false;
                }

                var workingGrid = new HeightGrid(renderTexture.width);
                var rawTextureData = workingTexture.GetRawTextureData<float>();
                rawTextureData.CopyTo(workingGrid.Values);

                var clusters = FindClusters(workingGrid);

                for (int i = 0; i < clusters.Count; i++)
                {
                    var cluster = clusters[i];

                    var mesh = CreateMesh(cluster, workingGrid, heightScale);

                    var indexedExportPath = FilesystemHelpers.GetIndexedFilePath(exportPath, i);
                    ExportMesh(mesh, indexedExportPath);
                }

                // Ensure the editor picks up any changes
                // NOTE: Unable to invoke a refresh directly during graph asset import
                EditorApplication.delayCall = () => AssetDatabase.Refresh();

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
            finally
            {
                if (workingTexture != null)
                {
                    Object.DestroyImmediate(workingTexture);
                    workingTexture = null;
                }
            }
        }

        private void ExportMesh(Mesh mesh, string outputFilePath)
        {
            var objData = MeshHelpers.GetObjData(mesh);

            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
            File.WriteAllText(outputFilePath, objData);
        }

        private List<List<Vector2Int>> FindClusters(HeightGrid grid)
        {
            var neighbors = new Vector2Int[]
            {
                new Vector2Int(-1, 0),
                new Vector2Int(1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, -1),
            };

            var clusters = new List<List<Vector2Int>>();
            var visited = new HashSet<Vector2Int>();

            var size = grid.Size;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2Int(x, y);

                    if (visited.Add(p))
                    {
                        if (grid[p.x, p.y] != 0)
                        {
                            var cluster = new List<Vector2Int>();
                            var queue = new Queue<Vector2Int>();

                            queue.Enqueue(p);

                            while (queue.Any())
                            {
                                var q = queue.Dequeue();

                                cluster.Add(q);

                                foreach (var neighbor in neighbors)
                                {
                                    var n = q + neighbor;

                                    if (n.x >= 0 && n.x < size &&
                                        n.y >= 0 && n.y < size)
                                    {
                                        if (visited.Add(n))
                                        {
                                            if (grid[n.x, n.y] != 0)
                                            {
                                                queue.Enqueue(n);
                                            }
                                        }
                                    }
                                }
                            }

                            clusters.Add(cluster);
                        }
                    }
                }
            }

            return clusters;
        }

        private Mesh CreateMesh(List<Vector2Int> cluster, HeightGrid grid, float heightScale)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            var indexMap = new Dictionary<Vector2Int, int>();

            var minHeight = float.MaxValue;
            var maxHeight = float.MinValue;
                
            foreach (var p in cluster)
            {
                var height = grid[p.x, p.y];

                minHeight = Mathf.Min(height, minHeight);
                maxHeight = Mathf.Max(height, maxHeight);
            }

            // Build vertex list
            foreach (var p in cluster)
            {
                var index = vertices.Count;
                indexMap[p] = index;

                var height = grid[p.x, p.y];
                var t = (height - minHeight) / (maxHeight - minHeight);
                var easedHeight = Mathf.Lerp(minHeight, maxHeight, Mathf.Pow(t, 0.75f));

                vertices.Add(new Vector3(p.x, easedHeight * heightScale, p.y));
            }

            // Connect neighbors into triangles
            foreach (var p in cluster)
            {
                if (indexMap.ContainsKey(new Vector2Int(p.x + 1, p.y)) &&
                    indexMap.ContainsKey(new Vector2Int(p.x, p.y + 1)) &&
                    indexMap.ContainsKey(new Vector2Int(p.x + 1, p.y + 1)))
                {
                    int i0 = indexMap[p];
                    int i1 = indexMap[new Vector2Int(p.x + 1, p.y)];
                    int i2 = indexMap[new Vector2Int(p.x, p.y + 1)];
                    int i3 = indexMap[new Vector2Int(p.x + 1, p.y + 1)];

                    triangles.AddRange(new[] { i0, i2, i1, i1, i2, i3 });
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();

            return mesh;
        }

    }
}
