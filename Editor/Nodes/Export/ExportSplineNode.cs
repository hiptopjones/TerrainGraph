using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

namespace CodeFirst.TerrainGraph.Editor
{
    [Serializable]
    public class ExportSplineNode
        : BaseNode<ExportSplineNode.OptionValues, ExportSplineNode.InputValues, NullOutput>, IExportableNode
    {
        public class OptionValues : OptionValuesBase
        {
            [DisplayName("Flatten")]
            public bool IsFlattened;
        }

        public class InputValues : InputValuesBase
        {
            [DisplayName("Spline")]
            public SplineWrapper SplineWrapper;

            [DefaultValue("My Spline")]
            [ValidIf(nameof(IsValidTarget))]
            public string TargetObjectName;
        }

        private ValidationResult IsValidTarget(InputValues inputs)
        {
            var classModel = ClassModelCache.GetClassModel<InputValues>();
            var targetModel = classModel.GetFieldModel(nameof(InputValues.TargetObjectName));

            if (string.IsNullOrEmpty(inputs.TargetObjectName))
            {
                return ValidationResult.Error($"{targetModel.DisplayName} input missing");
            }
            else
            {
                var splineContainers = Object.FindObjectsByType<SplineContainer>(FindObjectsInactive.Include);

                var namedSplineContainerCount = splineContainers.Count(x => x.name == inputs.TargetObjectName);
                if (namedSplineContainerCount == 0)
                {
                    return ValidationResult.Error($"{targetModel.DisplayName} input invalid");
                }
                else if (namedSplineContainerCount > 1)
                {
                    return ValidationResult.Error($"{targetModel.DisplayName} input ambiguous");
                }
            }

            return ValidationResult.Ok();
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

            var splineContainer = Object.FindObjectsByType<SplineContainer>(FindObjectsInactive.Include)
                .Single(x => x.name == inputTargetName);

            splineContainer.Spline = outputSpline;

            return true;
        }
    }
}
