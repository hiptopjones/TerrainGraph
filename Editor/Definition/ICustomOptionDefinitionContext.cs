using System;
using System.Linq.Expressions;
using System.Reflection;
using Unity.GraphToolkit.Editor;

namespace Indiecat.TerrainGraph.Editor
{
    public interface ICustomOptionDefinitionContext<TOptionValues>
    {
        ICustomOptionBuilder<TOption> AddOption<TOption>(Expression<Func<TOptionValues, TOption>> fieldExpression);
        ICustomOptionBuilder<TOption> AddOptionFromFieldInfo<TOption>(FieldInfo fieldInfo);
        INodeOption BuildOption<TOption>(Expression<Func<TOptionValues, TOption>> fieldExpression);
        INodeOption BuildOptionFromFieldInfo(FieldInfo fieldInfo);
    }
}
