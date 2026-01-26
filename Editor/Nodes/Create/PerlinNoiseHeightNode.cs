using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class PerlinNoiseHeightNode
        : BaseNode<PerlinNoiseHeightNode.OptionValues, PerlinNoiseHeightNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            public Vector2 Offset;

            [DefaultValue(0.05f)]
            public float Frequency;

            public int Seed;

            [MinValue(16), DefaultValue(256)]
            public int Size;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    Offset, Frequency, Seed, Size
                );
            }
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var offset = Inputs.Offset;
                var frequency = Inputs.Frequency;
                var seed = Inputs.Seed;
                var size = Inputs.Size;

                var start = NoiseHelpers.GetOffsetPositionInternal(offset, seed);

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader($"Shaders/{nameof(PerlinNoiseHeightNode)}", out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetVector("_Start", start);
                shader.SetFloat("_Frequency", frequency);

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
