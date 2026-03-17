using System;
using System.Runtime.InteropServices;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace CodeFirst.TerrainGraph.Editor
{
    [Serializable]
    [Node(categoryPath: "Create/Height/Spline", iconPath: null, title: "Spline Ridge Height")]
    public class SplineRidgeHeightNode
        : BaseNode<SplineRidgeHeightNode.OptionValues, SplineRidgeHeightNode.InputValues, HeightGrid>
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

            [DisplayName("Segments")]
            [MinValue(10), DefaultValue(100)]
            public int SegmentCount;

            [MinValue(5), DefaultValue(20)]
            public float BaseRadius;

            public AnimationCurve RadiusCurve;

            public AnimationCurve ProfileCurve;

            [DisplayName("Scale to Fit")]
            [IncludeIf(nameof(IsSplineBeingCentered))]
            public bool ScaleSplineToFit;

            [MinValue(16), DefaultValue(256)]
            public int Size;
        }

        private bool IsSplineBeingCentered() => Options.CenterSpline;

        protected override void OnDefineCustomInputPorts(IPortDefinitionContext context)
        {
            var classModel = ClassModelCache.GetClassModel<InputValues>();

            var profileModel = classModel.GetFieldModel(nameof(InputValues.ProfileCurve));
            profileModel.DefaultValue = AnimationCurve.EaseInOut(0, 0, 1, 1);

            var radiusModel = classModel.GetFieldModel(nameof(InputValues.RadiusCurve));
            radiusModel.DefaultValue = AnimationCurve.Linear(0, 1, 1, 1);

            // Build the ports automatically
            base.OnDefineCustomInputPorts(context);
        }

        protected override bool TryExecuteNodeInternal()
        {
            ComputeBuffer segmentsBuffer = null;
            Texture2D radiusCurveTexture = null;
            Texture2D profileCurveTexture = null;

            try
            {
                var centerSpline = Options.CenterSpline;
                var inputSplineWrapper = Inputs.SplineWrapper;
                var segmentCount = Inputs.SegmentCount;
                var baseRadius = Inputs.BaseRadius;
                var radiusCurve = Inputs.RadiusCurve;
                var profileCurve = Inputs.ProfileCurve;
                var scaleSplineToFit = Inputs.ScaleSplineToFit;
                var size = Inputs.Size;

                var inputSpline = inputSplineWrapper.Spline;

                var transformedSpline = SplineHelpers.GetTransformedSpline(inputSpline, size, centerSpline, scaleSplineToFit);
                var segments = SplineHelpers.GetSplineSegments(transformedSpline, segmentCount);

                int stride = Marshal.SizeOf(typeof(Segment));
                segmentsBuffer = new ComputeBuffer(segments.Count, stride);
                segmentsBuffer.SetData(segments);

                radiusCurveTexture = TextureHelpers.GetRampTexture(size, radiusCurve.Evaluate);
                profileCurveTexture = TextureHelpers.GetRampTexture(size, profileCurve.Evaluate);

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader(nameof(SplineRidgeHeightNode), out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetBuffer(kernel, "_Segments", segmentsBuffer);
                shader.SetInt("_SegmentCount", segmentCount);
                shader.SetFloat("_BaseRadius", baseRadius);
                shader.SetTexture(kernel, "_RadiusCurveTexture", radiusCurveTexture);
                shader.SetTexture(kernel, "_ProfileCurveTexture", profileCurveTexture);
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
                if (radiusCurveTexture != null)
                {
                    UnityObject.DestroyImmediate(radiusCurveTexture);
                    radiusCurveTexture = null;
                }

                if (profileCurveTexture != null)
                {
                    UnityObject.DestroyImmediate(profileCurveTexture);
                    profileCurveTexture = null;
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