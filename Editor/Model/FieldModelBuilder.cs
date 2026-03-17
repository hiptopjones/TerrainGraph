using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace CodeFirst.TerrainGraph.Editor
{
    public static class FieldModelBuilder
    {
        public static FieldModel BuildFieldModel(ClassModel classModel, FieldInfo fieldInfo)
        {
            var fieldModel = new FieldModel
            {
                ClassModel = classModel,

                Name = fieldInfo.Name,
                FieldType = fieldInfo.FieldType,
                DeclaringType = fieldInfo.DeclaringType,

                GetValue = CompileGetter(fieldInfo),
                SetValue = CompileSetter(fieldInfo),
            };

            // TODO: Fix this lookup
            if (fieldInfo.DeclaringType.Name.Contains("Option"))
            {
                fieldModel.PortName = $"{fieldInfo.Name}Option";
            }
            else
            {
                fieldModel.PortName = $"{fieldInfo.Name}Input";
            }

            // Check if this was declared on the base class (false) or the subclass (true)
            fieldModel.IsCustom = fieldInfo.DeclaringType == fieldInfo.ReflectedType;

            var attributes = fieldInfo.GetCustomAttributes().ToList();

            var displayNameAttribute = attributes.OfType<DisplayNameAttribute>().FirstOrDefault();
            fieldModel.DisplayName = displayNameAttribute?.DisplayName ?? StringHelpers.TitleCaseToWords(fieldInfo.Name);

            var defaultAttribute = attributes.OfType<DefaultValueAttribute>().FirstOrDefault();
            if (defaultAttribute != null)
            {
                fieldModel.DefaultValue = Convert.ChangeType(defaultAttribute.Value, fieldInfo.FieldType);
            }

            fieldModel.IsPassthru = attributes.OfType<PassthruAttribute>().Any();

            var includeIfAttribute = attributes.OfType<IncludeIfAttribute>().FirstOrDefault();
            if (includeIfAttribute != null)
            {
                fieldModel.IsIncluded = BuildInclusionDelegate(fieldInfo, includeIfAttribute.MethodName);
            }
            else
            {
                fieldModel.IsIncluded = _ => true;
            }

            fieldModel.IsIgnored = attributes.OfType<IgnoreAttribute>().Any();

            var minAttribute = attributes.OfType<MinValueAttribute>().FirstOrDefault();
            if (minAttribute != null)
            {
                fieldModel.Min = minAttribute.Min;
            }

            // TODO: Should warn the developer if both min and range are specified
            var rangeAttribute = attributes.OfType<RangeValueAttribute>().FirstOrDefault();
            if (rangeAttribute != null)
            {
                fieldModel.Min = rangeAttribute.Min;
                fieldModel.Max = rangeAttribute.Max;

                // TODO: Should warn the developer if specified without a range
                // Only check for a slider if a range is provided
                fieldModel.UseLinearSlider = attributes.OfType<SliderAttribute>().Any();

                if (fieldModel.Min >= 0 && fieldModel.Max <= 1)
                {
                    var powerSliderAttribute = attributes.OfType<PowerSliderAttribute>().FirstOrDefault();
                    if (powerSliderAttribute != null)
                    {
                        // TODO: Should warn the developer if specified with the wrong range
                        // Only check for power slider if range is within 0-1
                        fieldModel.UsePowerSlider = true;
                        fieldModel.PowerSliderPower = powerSliderAttribute.Power;
                    }
                }
            }

            var rules = new List<IValidationRule>();

            if (minAttribute != null)
            {
                rules.Add(new MinValueRule(fieldModel, minAttribute.Min));
            }

            if (rangeAttribute != null)
            {
                rules.Add(new RangeValueRule(fieldModel, rangeAttribute.Min, rangeAttribute.Max));
            }

            var notNullAttribute = attributes.OfType<NotNullAttribute>().FirstOrDefault();
            if (notNullAttribute != null)
            {
                rules.Add(new NotNullRule(fieldModel));
            }

            foreach (var validIf in attributes.OfType<ValidIfAttribute>())
            {
                rules.Add(new ValidIfRule(BuildValidationDelegate(fieldInfo, validIf.MethodName)));
            }

            fieldModel.Rules = rules;

            return fieldModel;
        }

        private static Func<object, bool> BuildInclusionDelegate(FieldInfo fieldInfo, string methodName)
        {
            var bindingFlags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            var reflectedType = fieldInfo.ReflectedType;
            var outerType = reflectedType.DeclaringType;

            var method = outerType.GetMethod(methodName, bindingFlags);
            return obj =>
            {
                try
                {
                    return (bool)method.Invoke(obj, null);
                }
                catch
                {
                    Debug.Log($"outer type: {outerType.FullName} invoking type: {obj.GetType().FullName}");
                    throw;
                }
            };
        }

        private static Func<object, object, ValidationResult> BuildValidationDelegate(FieldInfo fieldInfo, string methodName)
        {
            var bindingFlags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            var reflectedType = fieldInfo.ReflectedType;
            var outerType = reflectedType.DeclaringType;

            var method = outerType.GetMethod(methodName, bindingFlags);
            return (node, values) =>
            {
                try
                {
                    return (ValidationResult)method.Invoke(node, new object[] { values });
                }
                catch
                {
                    Debug.Log($"outer type: {outerType.FullName} invoking type: {node.GetType().FullName}");
                    throw;
                }
            };
        }

        private static Func<object, object> CompileGetter(FieldInfo fieldInfo)
        {
            // (object x) => (object)((TDecl)x).Field

            var objectParameter = Expression.Parameter(typeof(object), "x");
            var typedObjectParameter = Expression.Convert(objectParameter, fieldInfo.DeclaringType);
            var fieldExpression = Expression.Field(typedObjectParameter, fieldInfo);
            var boxedFieldExpression = Expression.Convert(fieldExpression, typeof(object));
            var lambdaExpression = Expression.Lambda<Func<object, object>>(boxedFieldExpression, objectParameter);

            return lambdaExpression.Compile();
        }

        private static Action<object, object> CompileSetter(FieldInfo fieldInfo)
        {
            // (object x, object v) => ((TDecl)x).Field = (TField)v

            var objectParameter = Expression.Parameter(typeof(object), "x");
            var valueParameter = Expression.Parameter(typeof(object), "v");

            var typedObjectParameter = Expression.Convert(objectParameter, fieldInfo.DeclaringType);
            var typedValueParameter = Expression.Convert(valueParameter, fieldInfo.FieldType);

            var assignmentExpression = Expression.Assign(Expression.Field(typedObjectParameter, fieldInfo), typedValueParameter);
            var lambdaExpression = Expression.Lambda<Action<object, object>>(assignmentExpression, objectParameter, valueParameter);

            return lambdaExpression.Compile();
        }
    }
}