using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    public class MinValueRule : IValidationRule
    {
        private readonly FieldModel _fieldModel;
        private readonly float _min;

        public MinValueRule(FieldModel fieldModel, float min)
        {
            _fieldModel = fieldModel;
            _min = min;
        }

        public ValidationResult Validate(object node, object values)
        {
            var rawValue = _fieldModel.GetValue(values);

            // Use float for both int and float cases, accepting limitations
            var floatValue = Convert.ToSingle(rawValue);
            var clampedValue = Mathf.Max(floatValue, _min);

            if (floatValue != clampedValue)
            {
                // No failure, just clamp
                _fieldModel.SetValue(values, Convert.ChangeType(clampedValue, _fieldModel.FieldType));

                return ValidationResult.Warning(
                    $"{_fieldModel.DisplayName} input invalid: {rawValue} (valid: n >= {_min})");
            }

            return ValidationResult.Ok();
        }
    }
}
