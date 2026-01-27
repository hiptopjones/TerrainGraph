using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class OpenClosedSplineNode
        : BaseNode<OpenClosedSplineNode.OptionValues, OpenClosedSplineNode.InputValues, SplineWrapper>
    {
        public enum OpenCloseOperation
        {
            OpenSpline,
            CloseSpline
        }

        public class OptionValues : OptionValuesBase
        {
            [DefaultValue(OpenCloseOperation.OpenSpline)]
            public OpenCloseOperation Operation;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    Operation
                );
            }
        }

        public class InputValues : InputValuesBase
        {
            [DisplayName("Spline")]
            [Passthru]
            public SplineWrapper SplineWrapper;

            [DefaultValue(true)]
            [IncludeIf(nameof(IsOperationOpen))]
            public bool AddLastVertex;

            [DefaultValue(true)]
            [IncludeIf(nameof(IsOperationClose))]
            public bool RemoveLastVertex;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    SplineWrapper?.VersionHash, AddLastVertex, RemoveLastVertex
                );
            }
        }

        private bool IsOperationOpen() => Options.Operation == OpenCloseOperation.OpenSpline;
        private bool IsOperationClose() => Options.Operation == OpenCloseOperation.CloseSpline;

        protected override bool TryExecuteNodeInternal()
        {
            try
            {
                var isClosingSpline = Options.Operation == OpenCloseOperation.CloseSpline;
                var inputSplineWrapper = Inputs.SplineWrapper;
                var addLastVertex = Inputs.AddLastVertex;
                var removeLastVertex = Inputs.RemoveLastVertex;

                var inputSpline = inputSplineWrapper.Spline;

                var vertices = inputSpline.Knots.Select(k => k.Position).ToList();

                Spline outputSpline;

                if (isClosingSpline)
                {
                    if (inputSpline.Closed)
                    {
                        outputSpline = inputSpline;
                    }
                    else
                    {
                        if (removeLastVertex)
                        {
                            vertices.RemoveAt(vertices.Count - 1);
                        }

                        outputSpline = SplineHelpers.CreateSpline(vertices, closed: true);
                    }
                }
                else
                {
                    if (!inputSpline.Closed)
                    {
                        outputSpline = inputSpline;
                    }
                    else
                    {
                        if (addLastVertex)
                        {
                            // Duplicate the first
                            vertices.Add(vertices.First());
                        }

                        outputSpline = SplineHelpers.CreateSpline(vertices, closed: false);
                    }
                }

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