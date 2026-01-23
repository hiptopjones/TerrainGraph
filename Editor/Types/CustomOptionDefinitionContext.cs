using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using static Unity.GraphToolkit.Editor.Node;

namespace Indiecat.TerrainGraph.Editor
{
    public class CustomOptionDefinitionContext<TOptions> : ICustomOptionDefinitionContext<TOptions>
    {
        private IOptionDefinitionContext _originalContext;

        public CustomOptionDefinitionContext(IOptionDefinitionContext originalContext)
        {
            _originalContext = originalContext;
        }

        public ICustomOptionBuilder<T> AddOption<T>(string name)
        {
            var builder = new CustomOptionBuilder<T>(name, _originalContext);
            return builder;
        }

        public ICustomOptionBuilder<T> AddOption<T>(Expression<Func<TOptions, T>> fieldExpression)
        {
            if (fieldExpression.Body is MemberExpression member &&
                (member.Member is FieldInfo || member.Member is PropertyInfo))
            {
                var memberInfo = member.Member;

                var name = $"{memberInfo.Name}Option";
                Debug.Log($"name: {name}");

                var builder = new CustomOptionBuilder<T>(name, _originalContext);

                // Try to add them all
                AddDisplayName(builder, memberInfo);
                AddDefaultValue(builder, memberInfo);
                AddRange(builder, memberInfo);

                return builder;
            }

            throw new ArgumentException("Invalid expression for options");
        }

        private void AddDisplayName<T>(CustomOptionBuilder<T> builder, MemberInfo memberInfo)
        {
            var attribute = memberInfo.GetCustomAttribute<DisplayNameAttribute>();
            var displayName = attribute?.DisplayName ?? StringHelpers.TitleCaseToWords(memberInfo.Name);

            Debug.Log($"display name: {displayName}");
            builder.WithDisplayName(displayName);
        }

        private void AddDefaultValue<T>(CustomOptionBuilder<T> builder, MemberInfo memberInfo)
        {
            var attribute = memberInfo.GetCustomAttribute<DefaultValueAttribute>();
            var defaultValue = attribute?.Value;

            if (defaultValue != default)
            {
                Debug.Log($"default value: {defaultValue}");
                builder.WithDefaultValue((T)defaultValue);
            }
        }

        private void AddRange<T>(CustomOptionBuilder<T> builder, MemberInfo memberInfo)
        {
            var attribute = memberInfo.GetCustomAttribute<RangeAttribute>();
            if (attribute != null)
            {
                if (typeof(T) == typeof(int) || typeof(T) == typeof(float))
                {
                    Debug.Log($"range: {attribute.min}, {attribute.max}");
                    builder.WithRange(
                        (T)Convert.ChangeType(attribute.min, typeof(T)),
                        (T)Convert.ChangeType(attribute.max, typeof(T)));
                }
            }
        }
    }
}
