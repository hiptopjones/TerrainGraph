using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CodeFirst.TerrainGraph.Editor
{
    [Serializable]
    [Node(categoryPath: "Create/Height/Simple", iconPath: null, title: "Gradient Height")]
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
        }

        protected override void OnDefineCustomInputPorts(IPortDefinitionContext context)
        {
            var classModel = ClassModelCache.GetClassModel<InputValues>();

            var gradientModel = classModel.GetFieldModel(nameof(InputValues.Gradient));
            gradientModel.DefaultValue = GradientHelpers.GetDefaultGradient();

            // Build the ports automatically
            base.OnDefineCustomInputPorts(context);
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

                if (!ComputeHelpers.TryLoadComputeShader(nameof(GradientHeightNode), out var shader))
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