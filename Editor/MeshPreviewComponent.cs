using Indiecat.UnityCommon.Runtime;
using UnityEditor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class MeshPreview : MonoBehaviour
    {
        [Range(16, 1024)]
        [SerializeField] private int _resolution = 256;

        [SerializeField] private int _heightScale = 100;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        private Material _previewMaterial;
        private RenderTexture _heightGridTexture;

        private int _lastResolution;

        void OnEnable()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();

            RegenerateMeshIfNeeded();
            EnsureMaterial();
        }

        void Update()
        {
            RegenerateMeshIfNeeded();
            UpdateMaterialProperties();
        }

        void RegenerateMeshIfNeeded()
        {
            if (_meshFilter.sharedMesh == null || _resolution != _lastResolution)
            {
                _meshFilter.sharedMesh = GenerateGridMesh(_resolution);
                _lastResolution = _resolution;

                transform.localScale = transform.localScale.WithX(_resolution).WithZ(_resolution);
            }
        }

        void EnsureMaterial()
        {
            var materialPath = "Materials/Height Grid Preview Material";

            _previewMaterial = Resources.Load<Material>(materialPath);
            if (_previewMaterial == null)
            {
                Debug.LogError($"Unable to find preview material: {materialPath}");
                return;
            }

            Debug.Log($"preview material found");
            _meshRenderer.sharedMaterial = _previewMaterial;
        }

        void UpdateMaterialProperties()
        {
            if (_heightGridTexture == null)
            {
                return;
            }
            
            var materialPropertyBlock = new MaterialPropertyBlock();

            materialPropertyBlock.SetTexture("_HeightGridTexture", _heightGridTexture);
            materialPropertyBlock.SetInt("_HeightScale", _heightScale);

            _meshRenderer.SetPropertyBlock(materialPropertyBlock);
        }

        public void SetHeightmap(RenderTexture renderTexture)
        {
            _heightGridTexture = renderTexture;

            // Check if we're disabled
            if (_meshRenderer != null)
            {
                UpdateMaterialProperties();

                SceneView.RepaintAll();
            }
        }

        public static Mesh GenerateGridMesh(int resolution)
        {
            var verticesPerSide = resolution + 1;
            var vertexCount = verticesPerSide * verticesPerSide;
            var triangleCount = resolution * resolution * 6;

            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            int[] triangles = new int[triangleCount];

            for (int y = 0; y < verticesPerSide; y++)
            {
                for (int x = 0; x < verticesPerSide; x++)
                {
                    var i = y * verticesPerSide + x;
                    var xf = (float)x / resolution;
                    var yf = (float)y / resolution;

                    vertices[i] = new Vector3(xf - 0.5f, 0, yf - 0.5f);
                    uvs[i] = new Vector2(xf, yf);
                }
            }

            int t = 0;
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    var i = y * verticesPerSide + x;

                    triangles[t++] = i;
                    triangles[t++] = i + verticesPerSide;
                    triangles[t++] = i + 1;

                    triangles[t++] = i + 1;
                    triangles[t++] = i + verticesPerSide;
                    triangles[t++] = i + verticesPerSide + 1;
                }
            }

            var mesh = new Mesh();
            mesh.indexFormat = vertexCount > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // Prevent saving this object in the scene
            mesh.hideFlags = HideFlags.HideAndDontSave;

            return mesh;
        }
    }

}
