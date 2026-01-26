using System;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ImportSplineNode
        : BaseNode<ImportSplineNode.OptionValues, ImportSplineNode.InputValues, SplineWrapper>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            [DefaultValue("My Spline")]
            [ValidIf(nameof(IsValidTarget))]
            public string TargetObjectName;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    TargetObjectName
                );
            }
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
                var splineContainers = Object.FindObjectsByType<SplineContainer>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);

                var namedSplineContainerCount = splineContainers.Count(x => x.name == inputs.TargetObjectName);
                if (namedSplineContainerCount == 0)
                {
                    return ValidationResult.Error($"{targetModel.DisplayName} input invalid");
                }
                else if (namedSplineContainerCount > 1)
                {
                    return ValidationResult.Error($"{targetModel.DisplayName} input ambiguous");
                }
                else
                {
                    var splineContainer = splineContainers.First();
                    if (splineContainer.Spline == null)
                    {
                        ValidationResult.Error($"{targetModel.DisplayName} missing spline");
                    }
                }
            }

            return ValidationResult.Ok();
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var inputTargetName = Inputs.TargetObjectName;

                var splineContainer = Object.FindObjectsByType<SplineContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Single(x => x.name == inputTargetName);

                var outputSpline = splineContainer.Spline;

                var outputSplineWrapper = new SplineWrapper
                {
                    Spline = outputSpline
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
    }
}
