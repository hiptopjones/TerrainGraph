using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace CodeFirst.TerrainGraph.Editor
{
    [Serializable]
    [Node(categoryPath: "Create/Height/Spline", iconPath: null, title: "Spline Height")]
    public class SplineHeightNode
        : BaseNode<SplineHeightNode.OptionValues, SplineHeightNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
            [DisplayName("Center")]
            public bool CenterSpline;
        }

        public class InputValues : InputValuesBase
        {
            [DisplayName("Spline")]
            public SplineWrapper SplineWrapper;

            [DisplayName("Samples")]
            [MinValue(10), DefaultValue(100)]
            public int SampleCount;

            public bool ApplySplineHeight;

            [DisplayName("Scale to Fit")]
            [IncludeIf(nameof(IsSplineBeingCentered))]
            public bool ScaleSplineToFit;

            [MinValue(16), DefaultValue(256)]
            public int Size;
        }

        private bool IsSplineBeingCentered() => Options.CenterSpline;

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var centerSpline = Options.CenterSpline;
                var inputSplineWrapper = Inputs.SplineWrapper;
                var sampleCount = Inputs.SampleCount;
                var scaleSplineToFit = Inputs.ScaleSplineToFit;
                var applySplineHeight = Inputs.ApplySplineHeight;
                var size = Inputs.Size;

                var inputSpline = inputSplineWrapper.Spline;
                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ShaderWrappers.TryGenerateSdf(
                    inputSpline,
                    size,
                    sampleCount,
                    centerSpline,
                    scaleSplineToFit,
                    applySplineHeight,
                    ref outputTexture))
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