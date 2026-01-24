using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ExportSplineNode
        : ExecutableNode<ExportSplineNode.OptionValues, ExportSplineNode.InputValues, NullOutput>
    {
        public class OptionValues : OptionValuesBase
        {
            [DisplayName("Flatten")]
            public bool IsFlattened;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    IsFlattened
                );
            }
        }

        public class InputValues : InputValuesBase
        {
            [DisplayName("Spline")]
            public SplineWrapper SplineWrapper;

            [DefaultValue("My Spline")]
            public string TargetObjectName;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    SplineWrapper?.VersionHash, TargetObjectName
                );
            }
        }

        protected override bool TryExecuteNodeInternal()
        {
            var isFlattened = Options.IsFlattened;
            var inputSplineWrapper = Inputs.SplineWrapper;
            var inputTargetName = Inputs.TargetObjectName;

            var inputSpline = inputSplineWrapper.Spline;

            Spline outputSpline = inputSpline;
            if (isFlattened)
            {
                var vertices = inputSpline.Knots.Select(k => new float3(k.Position.x, 0, k.Position.z));
                outputSpline = new Spline(vertices);
                outputSpline.Closed = inputSpline.Closed;
            }

            var splineContainer = Object.FindObjectsByType<SplineContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(x => x.name == inputTargetName);

            splineContainer.Spline = outputSpline;

            return true;
        }
    }
}
