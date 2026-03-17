using System;
using UnityEngine;

namespace CodeFirst.TerrainGraph.Editor
{
    [Serializable]
    public class RelaxNode
        : BaseNode<RelaxNode.OptionValues, RelaxNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            [Passthru]
            public HeightGrid Grid;

            [DisplayName("Iterations")]
            [RangeValue(1, 1000), DefaultValue(100)]
            public int IterationCount;
        }

        protected override bool TryExecuteNodeInternal()
        {
            RenderTexture tempTexture1 = null;
            RenderTexture tempTexture2 = null;

            try
            {
                var inputGrid = Inputs.Grid;
                var iterationCount = Inputs.IterationCount;

                var size = inputGrid.Size;

                var inputTexture = inputGrid.RenderTexture;

                // Create ping-pong textures
                tempTexture1 = TextureHelpers.CreateRenderTexture(size, RenderTextureFormat.RFloat);
                tempTexture2 = TextureHelpers.CreateRenderTexture(size, RenderTextureFormat.RFloat);

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader(nameof(RelaxNode), out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                var groups = Mathf.CeilToInt(size / 8.0f);

                Graphics.Blit(inputTexture, tempTexture1);

                for (int i = 0; i < iterationCount; i++)
                {
                    shader.SetTexture(kernel, "_InTexture", tempTexture1);
                    shader.SetTexture(kernel, "_OutTexture", tempTexture2);
                    shader.SetInt("_Size", size);

                    shader.Dispatch(kernel, groups, groups, 1);

                    var swapTexture = tempTexture1;
                    tempTexture1 = tempTexture2;
                    tempTexture2 = swapTexture;
                }

                Graphics.Blit(tempTexture1, outputTexture);

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
            finally
            {
                if (tempTexture1 != null)
                {
                    tempTexture1.Release();
                    tempTexture1 = null;
                }

                if (tempTexture2 != null)
                {
                    tempTexture2.Release();
                    tempTexture2 = null;
                }
            }
        }
    }
}