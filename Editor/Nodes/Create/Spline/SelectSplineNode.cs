using System;
using UnityEngine;

namespace CodeFirst.TerrainGraph.Editor
{
    [Serializable]
    public class SelectSplineNode
        : BaseNode<SelectSplineNode.OptionValues, SelectSplineNode.InputValues, SplineWrapper>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            public SplineListWrapper SplinesWrapper;

            [MinValue(0)]
            public int SplineIndex;
        }

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var inputSplinesWrapper = Inputs.SplinesWrapper;
                var splineIndex = Inputs.SplineIndex;

                var inputSplineWrappers = inputSplinesWrapper.Splines;
                if (inputSplineWrappers.Count <= splineIndex)
                {
                    Debug.LogError($"Spline index invalid ({inputSplineWrappers.Count} splines available)");
                    return false;
                }

                var outputSpline = inputSplineWrappers[splineIndex];

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
    }
}