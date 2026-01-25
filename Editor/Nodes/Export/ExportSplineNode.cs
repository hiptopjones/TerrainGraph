using System;
using System.Linq;
using Unity.GraphToolkit.Editor;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Windows;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ExportSplineNode
        : ExecutableNode<ExportSplineNode.OptionValues, ExportSplineNode.InputValues, NullOutput>, IExportableNode
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
            [ValidIf(nameof(IsValidTarget))]
            public string TargetObjectName;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    SplineWrapper?.VersionHash, TargetObjectName
                );
            }
        }

        private bool IsValidTarget(InputValues inputs, GraphLogger graphLogger)
        {
            var inputDisplayName = NodeHelpers.GetDisplayName(typeof(InputValues), nameof(InputValues.TargetObjectName));

            var isValid = true;

            if (string.IsNullOrEmpty(inputs.TargetObjectName))
            {
                graphLogger?.LogError($"{inputDisplayName} value missing", this);
                isValid = false;
            }
            else
            {
                var splineContainers = Object.FindObjectsByType<SplineContainer>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);

                var namedSplineContainerCount = splineContainers.Count(x => x.name == inputs.TargetObjectName);
                if (namedSplineContainerCount == 0)
                {
                    graphLogger?.LogError($"{inputDisplayName} value invalid", this);
                    isValid = false;
                }
                else if (namedSplineContainerCount > 1)
                {
                    graphLogger?.LogError($"{inputDisplayName} value ambiguous", this);
                    isValid = false;
                }
            }

            return isValid;
        }

        protected override bool TryExecuteNodeInternal()
        {
            return true;
        }

        public bool TryExportNode()
        {
            if (Inputs == null)
            {
                // Node is not in valid state
                return false;
            }

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
