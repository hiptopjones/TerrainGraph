using Indiecat.UnityCommon.Runtime;
using System;
using System.Linq;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

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
        }

        private SplineContainer _currentSplineContainer;

        // Changes when the spline changes, and injected into the hashcode calculation
        private float _uniqueSplineValue;

        public override void OnEnable()
        {
            EditorSplineUtility.AfterSplineWasModified += OnAfterSplineWasModified;
        }

        public override void OnDisable()
        {
            EditorSplineUtility.AfterSplineWasModified -= OnAfterSplineWasModified;
        }

        private ValidationResult IsValidTarget(InputValues inputs)
        {
            _currentSplineContainer = null;

            var classModel = ClassModelCache.GetClassModel<InputValues>();
            var targetModel = classModel.GetFieldModel(nameof(InputValues.TargetObjectName));

            if (string.IsNullOrEmpty(inputs.TargetObjectName))
            {
                return ValidationResult.Error($"{targetModel.DisplayName} input missing");
            }
            else
            {
                var namedSplineContainers = Object.FindObjectsByType<SplineContainer>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .Where(x => x.name == inputs.TargetObjectName).ToList();

                if (namedSplineContainers.Count == 0)
                {
                    return ValidationResult.Error($"{targetModel.DisplayName} input invalid");
                }
                else if (namedSplineContainers.Count > 1)
                {
                    return ValidationResult.Error($"{targetModel.DisplayName} input ambiguous");
                }

                var splineContainer = namedSplineContainers.Single();
                if (splineContainer.Spline == null)
                {
                    ValidationResult.Error($"{targetModel.DisplayName} missing spline");
                }

                _currentSplineContainer = splineContainer;

                return ValidationResult.Ok();
            }
        }

        private void OnAfterSplineWasModified(Spline spline)
        {
            if (_currentSplineContainer.IsUnityNull() || _currentSplineContainer.Spline != spline)
            {
                return;
            }

            // Will force the hashcode to be different, invalidating the node and forcing an update
            _uniqueSplineValue = Random.value;
        }

        protected override int GetInputsHashCode(InputValues inputs)
        {
            var hashCode = base.GetInputsHashCode(inputs);
            return HashCode.Combine(hashCode, _uniqueSplineValue);
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var inputTargetName = Inputs.TargetObjectName;

                var splineContainer = _currentSplineContainer;

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
