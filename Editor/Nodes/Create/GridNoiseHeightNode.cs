using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class GridNoiseHeightNode
        : BaseNode<GridNoiseHeightNode.OptionValues, GridNoiseHeightNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            public Vector2 Offset;

            [MinValue(5), DefaultValue(20)]
            public int CellSize;

            public int Seed;

            [MinValue(16), DefaultValue(256)]
            public int Size;
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var offset = Inputs.Offset;
                var cellSize = Inputs.CellSize;
                var seed = Inputs.Seed;
                var size = Inputs.Size;

                var start = GetOffsetPositionInternal(offset, seed);

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader(nameof(GridNoiseHeightNode), out var shader))
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

        public static Vector2 GetOffsetPositionInternal(Vector2 position, int seed)
        {
            Random.InitState(seed);

            var offsetX = Random.value * 200000 - 100000;
            var offsetY = Random.value * 200000 - 100000;

            // Offset the sampling position by a repeatable random location based on a seed
            position += new Vector2(offsetX, offsetY);

            return position;
        }
    }
}
