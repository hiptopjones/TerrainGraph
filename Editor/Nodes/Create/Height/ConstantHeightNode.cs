using System;
using UnityEngine;

namespace CodeFirst.TerrainGraph.Editor
{
    [Serializable]
    public class ConstantHeightNode
        : BaseNode<ConstantHeightNode.OptionValues, ConstantHeightNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            [DefaultValue(0.5f)]
            public float Height;

            [MinValue(16), DefaultValue(256)]
            public int Size;
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var height = Inputs.Height;
                var size = Inputs.Size;

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader(nameof(ConstantHeightNode), out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetFloat("_Height", height);

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