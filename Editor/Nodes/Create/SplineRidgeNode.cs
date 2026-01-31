using Indiecat.UnityCommon.Runtime;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class SplineRidgeNode
        : BaseNode<SplineRidgeNode.OptionValues, SplineRidgeNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            [DisplayName("Spline")]
            public SplineWrapper SplineWrapper;

            [DisplayName("Segments")]
            [MinValue(10), DefaultValue(100)]
            public int SegmentCount;

            [DisplayName("Center")]
            public bool IsCentered;

            [MinValue(5), DefaultValue(20)]
            public float BaseRadius;

            public AnimationCurve RadiusCurve;

            public AnimationCurve FalloffCurve;

            [MinValue(16), DefaultValue(256)]
            public int Size;
        }

        protected override void OnDefineCustomInputPorts(IPortDefinitionContext context)
        {
            var classModel = ClassModelCache.GetClassModel<InputValues>();

            var alloffCurveModel = classModel.GetFieldModel(nameof(InputValues.FalloffCurve));
            alloffCurveModel.DefaultValue = AnimationCurve.EaseInOut(0, 1, 1, 0);

            var radiusCurveModel = classModel.GetFieldModel(nameof(InputValues.RadiusCurve));
            radiusCurveModel.DefaultValue = AnimationCurve.Linear(0, 1, 1, 1);

            // Build the ports automatically
            base.OnDefineCustomInputPorts(context);
        }

        protected override bool TryExecuteNodeInternal()
        {
            ComputeBuffer segmentsBuffer = null;
            Texture2D radiusTexture = null;
            Texture2D falloffTexture = null;

            try
            {
                var inputSplineWrapper = Inputs.SplineWrapper;
                var segmentCount = Inputs.SegmentCount;
                var isCentered = Inputs.IsCentered;
                var baseRadius = Inputs.BaseRadius;
                var radiusCurve = Inputs.RadiusCurve;
                var falloffCurve = Inputs.FalloffCurve;
                var size = Inputs.Size;

                var inputSpline = inputSplineWrapper.Spline;

                List<Segment> segments;
                if (isCentered)
                {
                    var gridCenter = (Vector2.one * size / 2).ToVector3XZ();

                    var centeredSpline = SplineHelpers.GetCenteredSpline(inputSpline, gridCenter);
                    segments = SplineHelpers.GenerateSegments(centeredSpline, segmentCount);
                }
                else
                {
                    segments = SplineHelpers.GenerateSegments(inputSpline, segmentCount);
                }

                int stride = Marshal.SizeOf(typeof(Segment));
                segmentsBuffer = new ComputeBuffer(segments.Count, stride);
                segmentsBuffer.SetData(segments);

                radiusTexture = TextureHelpers.GetRampTexture(size, radiusCurve.Evaluate);
                falloffTexture = TextureHelpers.GetRampTexture(size, falloffCurve.Evaluate);

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader(nameof(SplineRidgeNode), out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetBuffer(kernel, "_Segments", segmentsBuffer);
                shader.SetInt("_SegmentCount", segmentCount);
                shader.SetFloat("_BaseRadius", baseRadius);
                shader.SetTexture(kernel, "_RadiusCurveTexture", radiusTexture);
                shader.SetTexture(kernel, "_FalloffCurveTexture", falloffTexture);
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
                if (radiusTexture != null)
                {
                    Object.DestroyImmediate(radiusTexture);
                    radiusTexture = null;
                }

                if (falloffTexture != null)
                {
                    Object.DestroyImmediate(falloffTexture);
                    falloffTexture = null;
                }

                if (segmentsBuffer != null)
                {
                    segmentsBuffer.Release();
                    segmentsBuffer = null;
                }
            }
        }
    }
}