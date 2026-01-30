using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class RampNode
        : BaseNode<RampNode.OptionValues, RampNode.InputValues, HeightGrid>
    {
        public enum RampType
        {
            Curve = 100,
            Gradient = 200
        }

        public class OptionValues : OptionValuesBase
        {
            [DefaultValue(RampType.Curve)]
            public RampType RampType;
        }

        public class InputValues : InputValuesBase
        {
            [Passthru]
            public HeightGrid Grid;

            [IncludeIf(nameof(IsRampTypeCurve))]
            public AnimationCurve Curve;

            [IncludeIf(nameof(IsRampTypeGradient))]
            public Gradient Gradient;
        }

        protected override void OnDefineCustomInputPorts(IPortDefinitionContext context)
        {
            var classModel = ClassModelCache.GetClassModel<InputValues>();

            var gradientModel = classModel.GetFieldModel(nameof(InputValues.Gradient));
            gradientModel.DefaultValue = GradientHelpers.GetDefaultGradient();

            var curveModel = classModel.GetFieldModel(nameof(InputValues.Curve));
            curveModel.DefaultValue = AnimationCurve.EaseInOut(0, 0, 1, 1);

            // Build the ports automatically
            base.OnDefineCustomInputPorts(context);
        }

        private bool IsRampTypeCurve() => Options.RampType == RampType.Curve;
        private bool IsRampTypeGradient() => Options.RampType == RampType.Gradient;

        protected override bool TryExecuteNodeInternal()
        {
            Texture2D rampTexture = null;

            try
            {
                var inputGrid = Inputs.Grid;

                var size = inputGrid.Size;

                var rampFunction = GetRampFunction();
                rampTexture = TextureHelpers.GetRampTexture(size, rampFunction);

                var inputTexture = inputGrid.RenderTexture;
                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader(nameof(RampNode), out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_InTexture", inputTexture);
                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetTexture(kernel, "_RampTexture", rampTexture);

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

        private Func<float, float> GetRampFunction()
        {
            var curve = Inputs.Curve;
            var gradient = Inputs.Gradient;

            switch (Options.RampType)
            {
                case RampType.Curve:
                    return (t) => curve.Evaluate(t);

                case RampType.Gradient:
                    return (t) => gradient.Evaluate(t).grayscale;

                default:
                    Debug.LogError($"Unhandled remap type: {Options.RampType}");
                    return (t) => 1;
            }
        }
    }
}