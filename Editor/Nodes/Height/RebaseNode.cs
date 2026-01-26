using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class RebaseNode
        : BaseNode<RebaseNode.OptionValues, RebaseNode.InputValues, HeightGrid>
    {
        public enum RebaseType
        {
            Floor = 100,
            Ceiling = 200
        }

        public class OptionValues : OptionValuesBase
        {
            [DefaultValue(RebaseType.Floor)]
            public RebaseType RebaseType;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    RebaseType
                );
            }
        }

        public class InputValues : InputValuesBase
        {
            [Passthru]
            public HeightGrid Grid;

            [DisplayName("Value")]
            public float TargetValue;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    Grid?.VersionHash, TargetValue
                );
            }
        }

        protected override void OnDefineCustomInputPorts(IPortDefinitionContext context)
        {
            var classModel = ClassModelCache.GetClassModel<InputValues>();

            var targetModel = classModel.GetFieldModel(nameof(InputValues.TargetValue));
            targetModel.DefaultValue = (Options.RebaseType == RebaseType.Floor ? 0 : 1);

            // Build the ports automatically
            base.OnDefineCustomInputPorts(context);
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var rebaseType = Options.RebaseType;
                var inputGrid = Inputs.Grid;
                var targetValue = Inputs.TargetValue;

                var size = inputGrid.Size;

                var inputTexture = inputGrid.RenderTexture;

                if (!ShaderWrappers.TryGetRange(inputTexture, out var rangeMin, out var rangeMax))
                {
                    return false;
                }

                var delta = targetValue - (rebaseType == RebaseType.Floor ? rangeMin : rangeMax);

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader($"Shaders/{nameof(RebaseNode)}", out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_InTexture", inputTexture);
                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetFloat("_Delta", delta);

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