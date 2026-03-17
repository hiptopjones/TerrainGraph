using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace CodeFirst.TerrainGraph.Editor
{
    [Serializable]
    [Node(categoryPath: "Modify/Height", iconPath: null, title: "Normalize")]
    public class NormalizeNode
        : BaseNode<NormalizeNode.OptionValues, NormalizeNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
            [DisplayName("Ignore Zero")]
            public bool IsZeroIgnored;
        }

        public class InputValues : InputValuesBase
        {
            [Passthru]
            public HeightGrid Grid;
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var isZeroIgnored = Options.IsZeroIgnored;
                var inputGrid = Inputs.Grid;

                var size = inputGrid.Size;

                var inputTexture = inputGrid.RenderTexture;

                if (!ShaderWrappers.TryGetRange(inputTexture, out var rangeMin, out var rangeMax))
                {
                    return false;
                }

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                var keywordBuilder = new KeywordBuilder();
                keywordBuilder.AddKeyword(isZeroIgnored ? "ZERO_EXCLUDE" : "ZERO_INCLUDE");

                if (!ComputeHelpers.TryLoadComputeShader(nameof(NormalizeNode), out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_InTexture", inputTexture);
                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetFloat("_RangeMin", rangeMin);
                shader.SetFloat("_RangeMax", rangeMax);

                shader.shaderKeywords = keywordBuilder.GetKeywords();

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