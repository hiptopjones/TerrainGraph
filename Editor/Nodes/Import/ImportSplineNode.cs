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
        : ExecutableNode<OptionValuesBase, ImportSplineNode.InputValues, SplineWrapper>
    {
        public class InputValues : InputValuesBase
        {
            [DefaultValue("My Spline")]
            [Validator(nameof(IsSplineFound))]
            public string TargetObjectName;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    TargetObjectName
                );
            }
        }

        private bool IsSplineFound(InputValues inputs, GraphLogger graphLogger)
        {
            // For error messages
            var inputDisplayName = NodeHelpers.GetDisplayName(typeof(InputValues), nameof(InputValues.TargetObjectName));

            var isValid = true;

            if (string.IsNullOrEmpty(inputs.TargetObjectName))
            {
                if (graphLogger != null) graphLogger.LogError($"{inputDisplayName} value missing", this);
                isValid = false;
            }
            else
            {
                var splineContainers = Object.FindObjectsByType<SplineContainer>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);

                var namedSplineContainerCount = splineContainers.Count(x => x.name == inputs.TargetObjectName);
                if (namedSplineContainerCount == 0)
                {
                    if (graphLogger != null) graphLogger.LogError($"{inputDisplayName} value invalid", this);
                    isValid = false;
                }
                else if (namedSplineContainerCount > 1)
                {
                    if (graphLogger != null) graphLogger.LogError($"{inputDisplayName} value ambiguous", this);
                    isValid = false;
                }
                else
                {
                    var splineContainer = splineContainers.First();
                    if (splineContainer.Spline == null)
                    {
                        if (graphLogger != null) graphLogger.LogError($"{inputDisplayName} missing spline", this);
                        isValid = false;
                    }
                }
            }

            return isValid;
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
