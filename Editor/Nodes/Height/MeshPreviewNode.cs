using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class MeshPreviewNode
        : BaseNode<MeshPreviewNode.OptionValues, MeshPreviewNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            [Passthru]
            public HeightGrid Grid;

            [DefaultValue("Grid Preview")]
            public string TargetObjectName;

            [MinValue(16), DefaultValue(256)]
            public int ReferenceSize;

            [MinValue(1), DefaultValue(100)]
            public int ReferenceHeight;
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var inputGrid = Inputs.Grid;
                var targetObjectName = Inputs.TargetObjectName;
                var referenceSize = Inputs.ReferenceSize;
                var referenceHeightScale = Inputs.ReferenceHeight;

                var size = inputGrid.Size;

                var inputTexture = inputGrid.RenderTexture;
                var outputTexture = GetOrCreateNodeRenderTexture(size);

                Graphics.Blit(inputTexture, outputTexture);

                var previewObject = GetOrCreatePreview(targetObjectName);
                previewObject.SetHeightTexture(outputTexture, referenceSize, referenceHeightScale);

                var outputGrid = new HeightGrid(size);

                outputGrid.RenderTexture = outputTexture;
                outputGrid.VersionHash = Inputs.VersionHash;

                CacheData.Output = outputGrid;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        public static MeshPreview GetOrCreatePreview(string targetName)
        {
            MeshPreview preview = Object.FindObjectsByType<MeshPreview>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(x => x.name == targetName);

            if (preview == null)
            {
                GameObject go = new GameObject(targetName);
                preview = go.AddComponent<MeshPreview>();
                go.transform.position = Vector3.zero;
            }

            return preview;
        }
    }
}
