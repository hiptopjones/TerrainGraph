using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ReplaceNode
        : BaseNode<ReplaceNode.OptionValues, ReplaceNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
            public override int GetHashCode()
            {
                // Avoid using the base hash code
                return 0;
            }
        }

        public class InputValues : InputValuesBase
        {
            [Passthru]
            public HeightGrid Grid;

            [DisplayName("From")]
            [DefaultValue(1f)]
            public float FromValue;

            [DisplayName("To")]
            [DefaultValue(0.5f)]
            public float ToValue;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    Grid?.VersionHash, FromValue, ToValue
                );
            }
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var inputGrid = Inputs.Grid;
                var fromValue = Inputs.FromValue;
                var toValue = Inputs.ToValue;

                var size = inputGrid.Size;

                var inputTexture = inputGrid.RenderTexture;
                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader(nameof(ReplaceNode), out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_InTexture", inputTexture);
                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetFloat("_From", fromValue);
                shader.SetFloat("_To", toValue);

                var groups = Mathf.CeilToInt(size / 8.0f);
                shader.Dispatch(kernel, groups, groups, 1);

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
    }
}