using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ExportMeshNode
        : BaseNode<ExportMeshNode.OptionValues, ExportMeshNode.InputValues, NullOutput>, IExportableNode
    {
        public class OptionValues : OptionValuesBase
        {
            public bool IgnoreZero;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    IgnoreZero
                );
            }
        }

        public class InputValues : InputValuesBase
        {
            public HeightGrid Grid;

            [DefaultValue(100)]
            public float HeightScale;

            [DefaultValue("Assets/Models/ExportedMesh.obj")]
            public string ExportPath;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    Grid?.VersionHash, HeightScale, ExportPath
                );
            }
        }

        protected override bool TryExecuteNodeInternal()
        {
            return true;
        }

        public bool TryExportNode()
        {
            if (Inputs == null)
            {
                // Node is not in valid state
                return false;
            }

            Texture2D workingTexture = null;

            try
            {
                var ignoreZero = Options.IgnoreZero;
                var inputGrid = Inputs.Grid;
                var heightScale = Inputs.HeightScale;
                var exportPath = Inputs.ExportPath;

                var renderTexture = inputGrid.RenderTexture;

                if (!TextureHelpers.TryCopyRenderTextureToTexture2D(renderTexture, TextureFormat.RFloat, out workingTexture))
                {
                    return false;
                }

                var size = renderTexture.width;

                var rawHeights = new float[size * size];
                var heights = new float[size, size];

                var rawTextureData = workingTexture.GetRawTextureData<float>();
                rawTextureData.CopyTo(rawHeights);

                GridHelpers.CopyHeights(rawHeights, heights);

                MeshHelpers.ExportMesh(heights, heightScale, ignoreZero, exportPath);

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
    }
}
