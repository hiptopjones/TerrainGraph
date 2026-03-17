using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace CodeFirst.TerrainGraph.Editor
{
    [Serializable]
    [Node(categoryPath: "Create/Height/Noise", iconPath: null, title: "Perlin Noise Height")]
    public class PerlinNoiseHeightNode
        : BaseNode<PerlinNoiseHeightNode.OptionValues, PerlinNoiseHeightNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            public Vector2 Offset;

            [RangeValue(0.00001f, 0.1f), DefaultValue(0.05f)]
            [PowerSlider]
            public float Frequency;

            public int Seed;

            [MinValue(16), DefaultValue(256)]
            public int Size;
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var offset = Inputs.Offset;
                var frequency = Inputs.Frequency;
                var seed = Inputs.Seed;
                var size = Inputs.Size;

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader(nameof(PerlinNoiseHeightNode), out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetVector("_Start", offset);
                shader.SetFloat("_Frequency", frequency);
                shader.SetInt("_Seed", seed);
                shader.SetInt("_Size", size);

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
