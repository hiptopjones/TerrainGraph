using Indiecat.UnityCommon.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class SplineRadialHeightNode
        : BaseNode<SplineRadialHeightNode.OptionValues, SplineRadialHeightNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            [DisplayName("Spline")]
            public SplineWrapper SplineWrapper;

            [DisplayName("Segments")]
            [MinValue(10), DefaultValue(50)]
            public int SegmentCount;

            [DisplayName("Profile")]
            public AnimationCurve ProfileCurve;

            [MinValue(16), DefaultValue(256)]
            public int Size;
        }

        protected override void OnDefineCustomInputPorts(IPortDefinitionContext context)
        {
            var classModel = ClassModelCache.GetClassModel<InputValues>();

            var curveModel = classModel.GetFieldModel(nameof(InputValues.ProfileCurve));
            curveModel.DefaultValue = AnimationCurve.EaseInOut(0, 0, 1, 1);

            // Build the ports automatically
            base.OnDefineCustomInputPorts(context);
        }

        protected override bool TryExecuteNodeInternal()
        {
            ComputeBuffer segmentsBuffer = null;
            Texture2D profileTexture = null;

            try
            {
                var inputSplineWrapper = Inputs.SplineWrapper;
                var segmentCount = Inputs.SegmentCount;
                var profileCurve = Inputs.ProfileCurve;
                var size = Inputs.Size;

                var inputSpline = inputSplineWrapper.Spline;

                List<Segment> segments;
                var gridCenter = (Vector2.one * size / 2).ToVector3XZ();

                var centeredSpline = SplineHelpers.GetCenteredSpline(inputSpline, gridCenter);
                segments = SplineHelpers.GenerateSegments(centeredSpline, segmentCount);

                int stride = Marshal.SizeOf(typeof(Segment));
                segmentsBuffer = new ComputeBuffer(segments.Count, stride);
                segmentsBuffer.SetData(segments);

                profileTexture = TextureHelpers.GetRampTexture(size, profileCurve.Evaluate);

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader(nameof(SplineRadialHeightNode), out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetTexture(kernel, "_ProfileTexture", profileTexture);
                shader.SetBuffer(kernel, "_Segments", segmentsBuffer);
                shader.SetInt("_SegmentCount", segments.Count);
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
                if (profileTexture != null)
                {
                    Object.DestroyImmediate(profileTexture);
                    profileTexture = null;
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