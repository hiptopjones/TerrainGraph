using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class BlurNode
        : BaseNode<BlurNode.OptionValues, BlurNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            [Passthru]
            public HeightGrid Grid;

            [MinValue(1), DefaultValue(5)]
            public int Radius;

            [RangeValue(1, 50), DefaultValue(1)]
            [DisplayName("Iterations")]
            public int IterationCount;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    Grid?.VersionHash, Radius, IterationCount
                );
            }
        }

        protected override bool TryExecuteNodeInternal()
        {
            ComputeBuffer weightBuffer = null;
            RenderTexture tempTexture1 = null;
            RenderTexture tempTexture2 = null;

            try
            {
                var inputGrid = Inputs.Grid;
                var radius = Inputs.Radius;
                var iterationCount = Inputs.IterationCount;

                var size = inputGrid.Size;

                var inputTexture = inputGrid.RenderTexture;

                var sigma = 3f;
                var weights = GetGaussianWeights(radius, sigma);

                weightBuffer = new ComputeBuffer(weights.Length, sizeof(float));
                weightBuffer.SetData(weights);

                // Create ping-pong textures
                tempTexture1 = TextureHelpers.CreateRenderTexture(size, RenderTextureFormat.RFloat);
                tempTexture2 = TextureHelpers.CreateRenderTexture(size, RenderTextureFormat.RFloat);

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader($"Shaders/{nameof(BlurNode)}", out var shader))
                {
                    return false;
                }

                var groups = Mathf.CeilToInt(size / 8.0f);

                Graphics.Blit(inputTexture, tempTexture1);

                for (int i = 0; i < iterationCount; i++)
                {
                    var horzKernel = shader.FindKernel("CSMain_Horizontal");

                    shader.SetTexture(horzKernel, "_InTexture", tempTexture1);
                    shader.SetTexture(horzKernel, "_OutTexture", tempTexture2);
                    shader.SetBuffer(horzKernel, "_Weights", weightBuffer);
                    shader.SetFloat("_Radius", radius);
                    shader.SetInt("_Size", size);
                    shader.Dispatch(horzKernel, groups, groups, 1);

                    var vertKernel = shader.FindKernel("CSMain_Vertical");

                    shader.SetTexture(vertKernel, "_InTexture", tempTexture2);
                    shader.SetTexture(vertKernel, "_OutTexture", tempTexture1);
                    shader.SetBuffer(vertKernel, "_Weights", weightBuffer);
                    shader.SetFloat("_Radius", radius);
                    shader.SetInt("_Size", size);
                    shader.Dispatch(vertKernel, groups, groups, 1);
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

                if (weightBuffer != null)
                {
                    weightBuffer.Release();
                    weightBuffer = null;
                }
            }
        }

        private float[] GetGaussianWeights(int radius, float sigma)
        {
            var weights = new float[radius * 2 + 1];

            var sum = 0f;

            for (int i = -radius; i <= radius; i++)
            {
                var w = Mathf.Exp(-(i * i) / (2 * sigma * sigma));
                weights[i + radius] = w;
                sum += w;
            }

            // Normalize so sum = 1
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] /= sum;
            }

            return weights;
        }
    }
}