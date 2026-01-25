using System;
using System.Linq.Expressions;
using System.Reflection;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using static Unity.GraphToolkit.Editor.Node;

namespace Indiecat.TerrainGraph.Editor
{
    public class CustomInputPortDefinitionContext<TInputValues> : ICustomInputPortDefinitionContext<TInputValues>
    {
        private IPortDefinitionContext _originalContext;

        public CustomInputPortDefinitionContext(IPortDefinitionContext originalContext)
        {
            _originalContext = originalContext;
        }

        public ICustomInputPortBuilder<TPort> AddInputPort<TPort>(Expression<Func<TInputValues, TPort>> fieldExpression)
        {
            var member = fieldExpression.Body as MemberExpression;
            var fieldInfo = member?.Member as FieldInfo;

            if (fieldInfo != null)
            {
                return AddInputPortFromFieldInfo<TPort>(fieldInfo);
            }

            throw new ArgumentException("Invalid expression");
        }

        public ICustomInputPortBuilder<TPort> AddInputPortFromFieldInfo<TPort>(FieldInfo fieldInfo)
        {
            var name = NodeHelpers.GetInputPortName(fieldInfo.Name);

            var builder = new CustomInputPortBuilder<TPort>(name, _originalContext);

            AddDisplayName(builder, fieldInfo);
            AddDefaultValue(builder, fieldInfo);
            AddRange(builder, fieldInfo);

            return builder;
        }

        public IPort BuildInputPort<TPort>(Expression<Func<TInputValues, TPort>> fieldExpression)
        {
            return AddInputPort(fieldExpression).Build();
        }

        public IPort BuildInputPortFromFieldInfo(FieldInfo fieldInfo)
        {
            var addInputPortMethod = GetType()
                .GetMethod(nameof(AddInputPortFromFieldInfo))
                .MakeGenericMethod(fieldInfo.FieldType);

            var builder = addInputPortMethod.Invoke(this, new object[] { fieldInfo });

            var buildMethod = builder.GetType().GetMethod("Build");
            return (IPort)buildMethod.Invoke(builder, new object[] { });
        }

        private void AddDisplayName<TPort>(ICustomInputPortBuilder<TPort> builder, FieldInfo fieldInfo)
        {
            var displayName = NodeHelpers.GetDisplayName(fieldInfo);
            builder.WithDisplayName(displayName);
        }

        private void AddDefaultValue<TPort>(ICustomInputPortBuilder<TPort> builder, FieldInfo fieldInfo)
        {
            var attribute = fieldInfo.GetCustomAttribute<DefaultValueAttribute>();

            if (attribute != null)
            {
                var defaultValue = attribute.Value;

                builder.WithDefaultValue((TPort)Convert.ChangeType(defaultValue, typeof(TPort)));
            }
        }

        private void AddRange<TPort>(ICustomInputPortBuilder<TPort> builder, FieldInfo fieldInfo)
        {
            var attribute = fieldInfo.GetCustomAttribute<RangeAttribute>();
            if (attribute != null)
            {
                if (typeof(TPort) == typeof(int) || typeof(TPort) == typeof(float))
                {
                    builder.WithRange(
                        (TPort)Convert.ChangeType(attribute.min, typeof(TPort)),
                        (TPort)Convert.ChangeType(attribute.max, typeof(TPort)));
                }
            }
        }
    }
}
