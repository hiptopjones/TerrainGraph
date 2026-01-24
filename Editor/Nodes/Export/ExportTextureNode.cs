using System;
using UnityEditor;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ExportTextureNode
        : ExecutableNode<OptionValuesBase, ExportTextureNode.InputValues, NullOutput>
    {
        public class InputValues : InputValuesBase
        {
            public HeightGrid Grid;

            [DisplayName("Path")]
            [DefaultValue("Assets/Textures/ExportedTexture.png")]
            public string ExportFilePath;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    Grid?.VersionHash, ExportFilePath
                );
            }
        }

        protected override bool TryExecuteNodeInternal()
        {
            var inputGrid = Inputs.Grid;
            var exportFilePath = Inputs.ExportFilePath;

            if (!TextureHelpers.TryExportHeightGridTexture(inputGrid, exportFilePath))
            {
                return false;
            }

            // Ensure the editor picks up any changes
            // NOTE: Unable to invoke a refresh directly during graph asset import
            EditorApplication.delayCall = () => AssetDatabase.Refresh();

            return true;
        }
    }
}
