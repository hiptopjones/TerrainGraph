using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class SplineHeightNode
        : BaseNode<SplineHeightNode.OptionValues, SplineHeightNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            [DisplayName("Spline")] public SplineWrapper SplineWrapper;

            [DisplayName("Samples")]
            [MinValue(10), DefaultValue(100)]
            public int SampleCount;

            [DisplayName("Center")]
            public bool IsCentered;

            public bool ApplySplineHeight;

            [MinValue(16), DefaultValue(256)]
            public int Size;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    SplineWrapper?.VersionHash, SampleCount, IsCentered, ApplySplineHeight, Size
                );
            }
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var inputSplineWrapper = Inputs.SplineWrapper;
                var sampleCount = Inputs.SampleCount;
                var isCentered = Inputs.IsCentered;
                var applySplineHeight = Inputs.ApplySplineHeight;
                var size = Inputs.Size;

                var inputSpline = inputSplineWrapper.Spline;
                RenderTexture outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ShaderWrappers.TryGenerateSdf(inputSpline, size, sampleCount, isCentered, applySplineHeight, ref outputTexture))
                {
                    return false;
                }

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