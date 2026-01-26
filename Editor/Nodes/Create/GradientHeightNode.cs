using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class GradientHeightNode
        : BaseNode<GradientHeightNode.OptionValues, GradientHeightNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            public Gradient Gradient;

            [MinValue(16), DefaultValue(256)]
            public int Size;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    GradientHelpers.GetHashCode(Gradient), Size
                );
            }
        }

        protected override bool TryExecuteNodeInternal()
        {
            Texture2D rampTexture = null;

            try
            {
                var gradient = Inputs.Gradient;
                var size = Inputs.Size;

                rampTexture = TextureHelpers.GetRampTexture(size, (t) => gradient.Evaluate(t).grayscale);
                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader($"Shaders/{nameof(GradientHeightNode)}", out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetTexture(kernel, "_RampTexture", rampTexture);
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
            finally
            {
                if (rampTexture != null)
                {
                    Object.DestroyImmediate(rampTexture);
                    rampTexture = null;
                }
            }
        }
    }
}