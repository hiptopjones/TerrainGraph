using UnityEditor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class MeshPreviewComponent : MonoBehaviour
    {
        private int _size = 256;
        private float _heightScale = 100;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        private Material _previewMaterial;
        private RenderTexture _heightGridTexture;

        private int _lastSize;

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
            if (_meshFilter.sharedMesh == null || _size != _lastSize)
            {
                var mesh = MeshHelpers.GenerateGridMesh(_size);

                // Prevent saving this object in the scene
                mesh.hideFlags = HideFlags.HideAndDontSave;

                _meshFilter.sharedMesh = mesh;

                _lastSize = _size;
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
            materialPropertyBlock.SetFloat("_HeightScale", _heightScale);

            _meshRenderer.SetPropertyBlock(materialPropertyBlock);
        }

        public void SetHeightTexture(RenderTexture renderTexture, int referenceSize, float referenceHeightScale)
        {
            _heightGridTexture = renderTexture;
            _size = _heightGridTexture.width;

            // Maintains a constant scale in the scene, regardless of grid size
            transform.localScale = Vector3.one * referenceSize / _size;
            _heightScale = referenceHeightScale * _size / referenceSize;

            if (_meshRenderer == null)
            {
                // Happens if we start out disabled
                return;
            }

            UpdateMaterialProperties();
            SceneView.RepaintAll();
        }
    }

}
