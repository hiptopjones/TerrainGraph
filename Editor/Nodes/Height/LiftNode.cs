using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class LiftNode
        : BaseNode<LiftNode.OptionValues, LiftNode.InputValues, HeightGrid>
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
                    EasingType
                );
            }
        }

        public class InputValues : InputValuesBase
        {
            [Passthru]
            public HeightGrid Grid;

            [DisplayName("Spline")]
            public SplineWrapper SplineWrapper;

            [DefaultValue(1)]
            public float Strength;

            public float Margin;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    Grid?.VersionHash, SplineWrapper?.VersionHash, Strength, Margin
                );
            }
        }

        protected override bool TryExecuteNodeInternal()
        {
            RenderTexture sdfTexture = null;

            try
            {
                var easingType = Options.EasingType;
                var inputGrid = Inputs.Grid;
                var inputSplineWrapper = Inputs.SplineWrapper;
                var strength = Inputs.Strength;
                var margin = Inputs.Margin;

                var size = inputGrid.Size;

                var inputSpline = inputSplineWrapper.Spline;
                var splineCenter = SplineHelpers.GetCenter(inputSpline);
                var sampleCount = (int)(inputSpline.GetLength() / 2);

                if (!ShaderWrappers.TryGenerateSdf(inputSpline, size, sampleCount, isCentered: false, applySplineHeight: false, ref sdfTexture))
                {
                    return false;
                }

                var keywordBuilder = new KeywordBuilder();
                keywordBuilder.AddKeyword($"EASING_{easingType.ToString().ToUpper()}");

                var inputTexture = inputGrid.RenderTexture;
                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader(nameof(LiftNode), out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_InTexture", inputTexture);
                shader.SetTexture(kernel, "_SdfTexture", sdfTexture);
                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetVector("_Center", splineCenter);
                shader.SetFloat("_Strength", strength);
                shader.SetFloat("_Margin", margin);
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
            finally
            {
                if (sdfTexture != null)
                {
                    Object.DestroyImmediate(sdfTexture);
                    sdfTexture = null;
                }
            }
        }
    }
}