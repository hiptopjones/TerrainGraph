using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
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
        }

        public class OptionValues : OptionValuesBase
        {
            [DefaultValue(ShapeType.Cone)]
            public ShapeType ShapeType;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    ShapeType
                );
            }
        }

        public class InputValues : InputValuesBase
        {
            [MinValue(0.0001f), DefaultValue(0.5f)]
            public float RadiusPercent;

            [MinValue(16), DefaultValue(256)]
            public int Size;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    RadiusPercent, Size
                );
            }
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var shapeType = Options.ShapeType;
                var radiusPercent = Inputs.RadiusPercent;
                var size = Inputs.Size;

                var radius = radiusPercent * size / 2;
                var center = new Vector2(size, size) / 2f;

                var keywordBuilder = new KeywordBuilder();
                keywordBuilder.AddKeyword($"OP_{shapeType.ToString().ToUpper()}");

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader($"Shaders/{nameof(RadialShapeHeightNode)}", out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetFloat("_Radius", radius);
                shader.SetVector("_Center", center);

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