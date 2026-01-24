using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class OpenClosedSplineNode
        : ExecutableNode<OpenClosedSplineNode.OptionValues, OpenClosedSplineNode.InputValues, SplineWrapper>
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
                    base.GetHashCode(),
                    Operation
                );
            }
        }

        public class InputValues : InputValuesBase
        {
            [DisplayName("Spline")]
            public SplineWrapper SplineWrapper;

            [DefaultValue(true)]
            public bool AddLastVertex;

            [DefaultValue(true)]
            public bool RemoveLastVertex;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    SplineWrapper?.VersionHash, AddLastVertex, RemoveLastVertex
                );
            }
        }

        protected override void OnDefineInputPorts(ICustomInputPortDefinitionContext<InputValues> context)
        {
            context.BuildInputPort(x => x.SplineWrapper);


            if (Options.Operation == OpenCloseOperation.OpenSpline)
            {
                context.BuildInputPort(x => x.AddLastVertex);
            }
            else
            {
                context.BuildInputPort(x => x.RemoveLastVertex);
            }
        }

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