using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class VoronoiNoiseHeightNode
        : BaseNode<VoronoiNoiseHeightNode.OptionValues, VoronoiNoiseHeightNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            public Vector2 Offset;

            [MinValue(1), DefaultValue(20), DisplayName("Points")]
            public int PointCount;

            public int Seed;

            [MinValue(16), DefaultValue(256)]
            public int Size;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    Offset, PointCount, Seed, Size
                );
            }
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var offset = Inputs.Offset;
                var pointCount = Inputs.PointCount;
                var seed = Inputs.Seed;
                var size = Inputs.Size;

                var start = NoiseHelpers.GetOffsetPositionInternal(offset, seed);

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader($"Shaders/{nameof(VoronoiNoiseHeightNode)}", out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetFloat("_PointCount", pointCount);
                shader.SetVector("_Start", start);
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
