using System;
using System.Linq.Expressions;

namespace Indiecat.TerrainGraph.Editor
{
    public interface ICustomOptionDefinitionContext<TOptions>
    {
        ICustomOptionBuilder<TData> AddOption<TData>(string name);
        ICustomOptionBuilder<T> AddOption<T>(Expression<Func<TOptions, T>> fieldExpression);
    }
}
