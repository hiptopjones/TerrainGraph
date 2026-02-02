using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class SplineVoronoiHeightNode
        : BaseNode<SplineVoronoiHeightNode.OptionValues, SplineVoronoiHeightNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
            [DisplayName("Use Sampled Points")]
            public bool IsSamplingEnabled;
        }

        public class InputValues : InputValuesBase
        {
            [DisplayName("Spline")]
            public SplineWrapper SplineWrapper;

            [DisplayName("Samples")]
            [MinValue(10), DefaultValue(100)]
            [IncludeIf(nameof(IsSamplingEnabled))]
            public int SampleCount;

            [MinValue(16), DefaultValue(256)]
            public int Size;
        }

        private bool IsSamplingEnabled() => Options.IsSamplingEnabled;

        protected override bool TryExecuteNodeInternal()
        {
            ComputeBuffer pointsBuffer = null;

            try
            {
                var isSamplingEnabled = Options.IsSamplingEnabled;
                var inputSplineWrapper = Inputs.SplineWrapper;
                var sampleCount = Inputs.SampleCount;
                var size = Inputs.Size;

                var inputSpline = inputSplineWrapper.Spline;

                List<Vector3> points = null;

                if (isSamplingEnabled)
                {
                    points = SplineHelpers.GetSplineVertices3d(inputSpline, sampleCount);
                }
                else
                {
                    points = inputSpline.Knots.Select(k => (Vector3)k.Position).ToList();
                }

                pointsBuffer = new ComputeBuffer(points.Count, sizeof(float) * 3);
                pointsBuffer.SetData(points);

                RenderTexture outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader(nameof(SplineVoronoiHeightNode), out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetBuffer(kernel, "_Points", pointsBuffer);
                shader.SetFloat("_PointsCount", points.Count);
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
                if (pointsBuffer != null)
                {
                    pointsBuffer.Release();
                    pointsBuffer = null;
                }
            }
        }
    }
}