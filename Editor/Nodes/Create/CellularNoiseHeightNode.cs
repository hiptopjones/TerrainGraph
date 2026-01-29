using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class CellularNoiseHeightNode
        : BaseNode<CellularNoiseHeightNode.OptionValues, CellularNoiseHeightNode.InputValues, HeightGrid>
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
            public Vector2 Offset;

            [MinValue(5), DefaultValue(20)]
            public int CellSize;
            public int Seed;

            [MinValue(16), DefaultValue(256)]
            public int Size;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    Offset, CellSize, Seed, Size
                );
            }
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var offset = Inputs.Offset;
                var cellSize = Inputs.CellSize;
                var seed = Inputs.Seed;
                var size = Inputs.Size;

                var start = NoiseHelpers.GetOffsetPositionInternal(offset, seed);

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader(nameof(CellularNoiseHeightNode), out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetVector("_Start", start);
                shader.SetFloat("_CellSize", cellSize);

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
