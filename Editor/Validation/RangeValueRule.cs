using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    public class RangeValueRule : IValidationRule
    {
        private readonly FieldModel _fieldModel;
        private readonly float _min;
        private readonly float _max;

        public RangeValueRule(FieldModel fieldModel, float min, float max)
        {
            _fieldModel = fieldModel;
            _min = min;
            _max = max;
        }

        public ValidationResult Validate(object node, object values)
        {
            var rawValue = _fieldModel.GetValue(values);

            // Use float for both int and float cases, accepting limitations
            var floatValue = Convert.ToSingle(rawValue);
            var clampedValue = Mathf.Clamp(floatValue, _min, _max);

            if (floatValue != clampedValue)
            {
                // No failure, just clamp
                _fieldModel.SetValue(values, Convert.ChangeType(clampedValue, _fieldModel.FieldType));

                return ValidationResult.Warning(
                    $"{_fieldModel.DisplayName} input invalid: {rawValue} (valid: n >= {_min} && n <= {_max})");
            }

            return ValidationResult.Ok();
        }
    }
}
