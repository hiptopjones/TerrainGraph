using System;
using System.Linq.Expressions;
using System.Reflection;
using Unity.GraphToolkit.Editor;

namespace Indiecat.TerrainGraph.Editor
{
    public interface ICustomInputPortDefinitionContext<TInputValues>
    {
        ICustomInputPortBuilder<TPort> AddInputPort<TPort>(Expression<Func<TInputValues, TPort>> fieldExpression);
        IPort BuildInputPort<TPort>(Expression<Func<TInputValues, TPort>> fieldExpression);
    }
}
