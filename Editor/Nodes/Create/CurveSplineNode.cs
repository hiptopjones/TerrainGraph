using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Indiecat.TerrainGraph.Editor
{
    using CurveType = CurveFunctions.CurveType;

    [Serializable]
    public class CurveSplineNode
        : BaseNode<CurveSplineNode.OptionValues, CurveSplineNode.InputValues, SplineWrapper>
    {
        public class OptionValues : OptionValuesBase
        {
            public CurveType CurveType;
        }

        public class InputValues : InputValuesBase
        {
            [DefaultValue(CurveType.Line)]
            public CurveType CurveType;

            [MinValue(16), DefaultValue(256)]
            public int Size;

            [DisplayName("Vertices")]
            [MinValue(10), DefaultValue(10)]
            public int VertexCount;
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var curveType = Inputs.CurveType;
                var size = Inputs.Size;
                var vertexCount = Inputs.VertexCount;

                if (!TryGetSpline(curveType, size, vertexCount, out var outputSpline))
                {
                    return false;
                }

                var outputSplineWrapper = new SplineWrapper
                {
                    Spline = outputSpline,
                };

                outputSplineWrapper.VersionHash = Inputs.VersionHash;

                CacheData.Output = outputSplineWrapper;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        public bool TryGetSpline(CurveType curveType, int size, int vertexCount, out Spline spline)
        {
            spline = null;

            if (!CurveFunctions.TryGetFunction(curveType, out var curveFunction))
            {
                return false;
            }

            var vertices = new List<Vector2>();

            for (int i = 0; i < vertexCount; i++)
            {
                var t = i / (float)(vertexCount - 1);
                var vertex = curveFunction(t) * size;
                vertices.Add(vertex);
            }

            spline = new Spline(vertices.Select(p => new float3(p.x, 0, p.y)));
            return true;
        }
    }
}