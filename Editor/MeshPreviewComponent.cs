using UnityEngine;

namespace CodeFirst.TerrainGraph.Editor
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class MeshPreviewComponent : MonoBehaviour
    {
        [SerializeField] private int _referenceSize = 256;
        [SerializeField] private float _referenceHeightScale = 100;

        private int _size = 256;
        private float _heightScale = 100;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        private Material _previewMaterial;
        private RenderTexture _heightGridTexture;

        private int _lastSize;

        private void OnEnable()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();

            RegenerateMeshIfNeeded();
            EnsureMaterial();
        }

        private void Update()
        {
            RegenerateMeshIfNeeded();
            UpdateMaterialProperties();
        }

        private void RegenerateMeshIfNeeded()
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

        private void EnsureMaterial()
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

        private void UpdateMaterialProperties()
        {
            if (_heightGridTexture == null || _meshRenderer == null)
            {
                return;
            }

            var materialPropertyBlock = new MaterialPropertyBlock();

            materialPropertyBlock.SetTexture("_HeightGridTexture", _heightGridTexture);
            materialPropertyBlock.SetFloat("_HeightScale", _heightScale);

            _meshRenderer.SetPropertyBlock(materialPropertyBlock);
        }

        public void SetHeightTexture(RenderTexture renderTexture)
        {
            // NOTE:
            // Output render textures are generally tied to a specific node instance
            // The texture reference should change only if the grid size changes
            // The mesh will reflect the texture as long as the reference is valid

            _heightGridTexture = renderTexture;

            _size = _heightGridTexture.width;

            // Maintains a constant scale in the scene, regardless of grid size
            transform.localScale = Vector3.one * _referenceSize / _size;
            _heightScale = _referenceHeightScale * _size / _referenceSize;
        }
    }
}
