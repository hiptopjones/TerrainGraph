using System;
using System.Linq.Expressions;
using System.Reflection;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using static Unity.GraphToolkit.Editor.Node;

namespace Indiecat.TerrainGraph.Editor
{
    public class CustomOptionDefinitionContext<TOptionValues> : ICustomOptionDefinitionContext<TOptionValues>
    {
        private IOptionDefinitionContext _originalContext;

        public CustomOptionDefinitionContext(IOptionDefinitionContext originalContext)
        {
            _originalContext = originalContext;
        }

        public ICustomOptionBuilder<TOption> AddOption<TOption>(Expression<Func<TOptionValues, TOption>> fieldExpression)
        {
            var member = fieldExpression.Body as MemberExpression;
            var fieldInfo = member?.Member as FieldInfo;

            if (fieldInfo != null)
            {
                // TODO: This format should be provided by a helper shared by ExecutableNode
                var name = $"{fieldInfo.Name}Option";

                var builder = new CustomOptionBuilder<TOption>(name, _originalContext);

                // Try to add them all
                AddDisplayName(builder, fieldInfo);
                AddDefaultValue(builder, fieldInfo);
                AddRange(builder, fieldInfo);

                return builder;
            }

            throw new ArgumentException("Invalid expression");
        }

        public INodeOption BuildOption<TPort>(Expression<Func<TOptionValues, TPort>> fieldExpression)
        {
            return AddOption(fieldExpression).Build();
        }

        private void AddDisplayName<TOption>(ICustomOptionBuilder<TOption> builder, FieldInfo fieldInfo)
        {
            var attribute = fieldInfo.GetCustomAttribute<DisplayNameAttribute>();
            var displayName = attribute?.DisplayName ?? StringHelpers.TitleCaseToWords(fieldInfo.Name);

            builder.WithDisplayName(displayName);
        }

        private void AddDefaultValue<TOption>(ICustomOptionBuilder<TOption> builder, FieldInfo fieldInfo)
        {
            var attribute = fieldInfo.GetCustomAttribute<DefaultValueAttribute>();
            var defaultValue = attribute?.Value;

            if (defaultValue != default)
            {
                builder.WithDefaultValue((TOption)defaultValue);
            }
        }

        private void AddRange<TOption>(ICustomOptionBuilder<TOption> builder, FieldInfo fieldInfo)
        {
            var attribute = fieldInfo.GetCustomAttribute<RangeAttribute>();
            if (attribute != null)
            {
                if (typeof(TOption) == typeof(int) || typeof(TOption) == typeof(float))
                {
                    builder.WithRange(
                        (TOption)Convert.ChangeType(attribute.min, typeof(TOption)),
                        (TOption)Convert.ChangeType(attribute.max, typeof(TOption)));
                }
            }
        }
    }
}
