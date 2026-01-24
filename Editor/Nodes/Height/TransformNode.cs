using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class TransformNode
        : ExecutableNode<OptionValuesBase, TransformNode.InputValues, HeightGrid>
    {
        public class InputValues : InputValuesBase
        {
            public HeightGrid Grid;

            public Vector2 TranslationPercent;

            public float RotationDegrees;

            public Vector2 Scale;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    Grid?.VersionHash, TranslationPercent, RotationDegrees, Scale
                );
            }
        }

        protected override void OnDefineInputPorts(ICustomInputPortDefinitionContext<InputValues> context)
        {
            context.BuildInputPort(x => x.Grid);
            context.BuildInputPort(x => x.TranslationPercent);
            context.BuildInputPort(x => x.RotationDegrees);

            context.AddInputPort(x => x.Scale)
                .WithDefaultValue(Vector2.one)
                .Build();
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var inputGrid = Inputs.Grid;
                var translationPercent = Inputs.TranslationPercent;
                var rotationDegrees = Inputs.RotationDegrees;
                var scale = Inputs.Scale;

                var size = inputGrid.Size;
                var translation = translationPercent * size;
                var center = Vector2.one * size / 2f;

                var trs = Matrix4x4.TRS(
                    new Vector3(-translation.x, translation.y, 0),
                    Quaternion.Euler(0, 0, rotationDegrees),
                    new Vector3(1 / scale.x, 1 / scale.y, 1));

                var inputTexture = inputGrid.RenderTexture;
                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader($"Shaders/{nameof(TransformNode)}", out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_InTexture", inputTexture);
                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetFloat("_Size", size);
                shader.SetVector("_Center", center);
                shader.SetMatrix("_Transform", trs);

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