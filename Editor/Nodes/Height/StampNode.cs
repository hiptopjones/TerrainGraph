using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class StampNode
        : ExecutableNode<StampNode.OptionValues, StampNode.InputValues, HeightGrid>
    {
        public enum EasingType
        {
            Constant = 100,
            Linear = 200,
            Cubic = 300,
            SmoothStep = 400,
        }

        public class OptionValues : OptionValuesBase
        {
            [DefaultValue(EasingType.SmoothStep)]
            public EasingType EasingType;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    EasingType
                );
            }
        }


        public class InputValues : InputValuesBase
        {
            public HeightGrid Grid;

            [DisplayName("Stamp")]
            public HeightGrid StampGrid;

            [DisplayName("Mask")]
            public HeightGrid MaskGrid;

            [DefaultValue(10)]
            public int Radius;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    Grid?.VersionHash, StampGrid?.VersionHash, MaskGrid?.VersionHash, Radius
                );
            }
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var easingType = Options.EasingType;
                var inputGrid = Inputs.Grid;
                var stampGrid = Inputs.StampGrid;
                var maskGrid = Inputs.MaskGrid;
                var radius = Inputs.Radius;

                var size = inputGrid.Size;

                var keywordBuilder = new KeywordBuilder();
                keywordBuilder.AddKeyword($"EASING_{easingType.ToString().ToUpper()}");

                var inputTexture = inputGrid.RenderTexture;
                var stampTexture = stampGrid.RenderTexture;
                var maskTexture = maskGrid.RenderTexture;
                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader($"Shaders/{nameof(StampNode)}", out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_InTexture", inputTexture);
                shader.SetTexture(kernel, "_StampTexture", stampTexture);
                shader.SetTexture(kernel, "_MaskTexture", maskTexture);
                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetInt("_Radius", radius);
                shader.SetInt("_Size", size);

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