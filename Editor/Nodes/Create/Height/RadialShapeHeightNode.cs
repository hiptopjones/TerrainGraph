using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CodeFirst.TerrainGraph.Editor
{
    [Serializable]
    public class RadialShapeHeightNode
        : BaseNode<RadialShapeHeightNode.OptionValues, RadialShapeHeightNode.InputValues, HeightGrid>
    {
        public enum ShapeType
        {
            Cone = 100,
            Cylinder = 200,
            Gaussian = 300,
            SmoothStep = 400,
            Custom = 1000,
        }

        public class OptionValues : OptionValuesBase
        {
            [DefaultValue(ShapeType.Cone)]
            public ShapeType ShapeType;
        }

        public class InputValues : InputValuesBase
        {
            [RangeValue(0.0001f, 1), DefaultValue(0.5f)]
            [Slider]
            [DisplayName("Radius")]
            public float RadiusPercent;

            [IncludeIf(nameof(IsShapeTypeCustom))]
            [DisplayName("Profile")]
            public AnimationCurve ProfileCurve;

            [MinValue(16), DefaultValue(256)]
            public int Size;
        }

        private bool IsShapeTypeCustom() => Options.ShapeType == ShapeType.Custom;

        protected override void OnDefineCustomInputPorts(IPortDefinitionContext context)
        {
            var classModel = ClassModelCache.GetClassModel<InputValues>();

            var profileModel = classModel.GetFieldModel(nameof(InputValues.ProfileCurve));
            profileModel.DefaultValue = AnimationCurve.EaseInOut(0, 0, 1, 1);

            // Build the ports automatically
            base.OnDefineCustomInputPorts(context);
        }

        protected override bool TryExecuteNodeInternal()
        {
            Texture2D profileCurveTexture = null;

            try
            {
                var shapeType = Options.ShapeType;
                var radiusPercent = Inputs.RadiusPercent;
                var profileCurve = Inputs.ProfileCurve;
                var size = Inputs.Size;

                var radius = radiusPercent * size / 2;
                var center = new Vector2(size, size) / 2f;

                var keywordBuilder = new KeywordBuilder();
                keywordBuilder.AddKeyword($"OP_{shapeType.ToString().ToUpper()}");

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader(nameof(RadialShapeHeightNode), out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetFloat("_Radius", radius);
                shader.SetVector("_Center", center);

                if (IsShapeTypeCustom())
                {
                    profileCurveTexture = TextureHelpers.GetRampTexture(size, profileCurve.Evaluate);
                    shader.SetTexture(kernel, "_ProfileCurveTexture", profileCurveTexture);
                }

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
                if (profileCurveTexture != null)
                {
                    Object.DestroyImmediate(profileCurveTexture);
                    profileCurveTexture = null;
                }
            }
        }
    }
}