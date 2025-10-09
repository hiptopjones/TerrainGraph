using System.IO;
using System.Text;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    public static class MeshHelpers
    {
        public class MeshData
        {
            public Vector3[] Vertices;
            public int[] Triangles;
            public Vector2[] Uvs;

            public MeshData()
            {
            }

            public MeshData(int width, int height)
            {
                Vertices = new Vector3[width * height];
                Uvs = new Vector2[width * height];

                // 6 because there are 2 triangles per quad, and each has 3 vertices
                Triangles = new int[(width - 1) * (height - 1) * 6];
            }
        }

        public static void ExportMesh(float[,] heights, float heightMultiplier, string outputFilePath)
        {
            var meshData = TessellateGrid(heights, heightMultiplier);
            var objData = GetObjData(meshData);

            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
            File.WriteAllText(outputFilePath, objData);
        }

        private static MeshData TessellateGrid(float[,] heights, float heightMultiplier)
        {
            int size = heights.GetLength(0);

            MeshData meshData = new MeshData(size, size);

            int vertexIndex = 0;
            int triangleIndex = 0;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float value = heights[x, y] * heightMultiplier;

                    meshData.Vertices[vertexIndex] = new Vector3(x, value, y);

                    // No triangles on the last row/column of vertices
                    if (x < size - 1 && y < size - 1)
                    {
                        // Consistent triangle winding is important for consistent face culling

                        // Left triangle of quad
                        meshData.Triangles[triangleIndex++] = vertexIndex;
                        meshData.Triangles[triangleIndex++] = vertexIndex + size;
                        meshData.Triangles[triangleIndex++] = vertexIndex + size + 1;

                        // Right triangle of quad
                        meshData.Triangles[triangleIndex++] = vertexIndex;
                        meshData.Triangles[triangleIndex++] = vertexIndex + size + 1;
                        meshData.Triangles[triangleIndex++] = vertexIndex + 1;
                    }

                    meshData.Uvs[vertexIndex] = new Vector2((float)x / size, (float)y / size);

                    vertexIndex++;
                }
            }

            return meshData;
        }

        public static string GetObjData(Mesh mesh)
        {
            var meshData = new MeshData
            {
                Vertices = mesh.vertices,
                Uvs = mesh.uv,
                Triangles = mesh.triangles
            };

            return GetObjData(meshData);
        }

        public static string GetObjData(MeshData meshData)
        {
            var builder = new StringBuilder();

            // Write vertices
            foreach (Vector3 v in meshData.Vertices)
            {
                // X is flipped to make the model use a right-handed coordinate system, which
                // is necessary to make Unity display the model properly on import
                builder.AppendFormat("v {0} {1} {2}\n", -v.x, v.y, v.z);
            }

            builder.Append("\n");

            if (meshData.Uvs != null && meshData.Uvs.Length != 0)
            {
                // Write UVs
                foreach (Vector2 uv in meshData.Uvs)
                {
                    builder.AppendFormat("vt {0} {1}\n", uv.x, uv.y);
                }

                builder.Append("\n");
            }

            // Write triangles
            for (int i = 0; i < meshData.Triangles.Length; i += 3)
            {
                // Order of vertices here reflects the winding in a right-handed coordinate system, which
                // is necessary to make Unity display the model properly on import
                // (Note that the OBJ format uses 1-based vertex indexing, so we add 1 to the indices below)
                builder.AppendFormat("f {0}/{0}/{0} {2}/{2}/{2} {1}/{1}/{1}\n",
                    meshData.Triangles[i] + 1,
                    meshData.Triangles[i + 1] + 1,
                    meshData.Triangles[i + 2] + 1);
            }

            builder.Append("\n");
            return builder.ToString();
        }
    }
}
