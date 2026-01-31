namespace Indiecat.TerrainGraph.Editor
{
    public class NotNullRule : IValidationRule
    {
        private readonly FieldModel _fieldModel;

        public NotNullRule(FieldModel fieldModel)
        {
            _fieldModel = fieldModel;
        }

        public ValidationResult Validate(object node, object values)
        {
            var value = _fieldModel.GetValue(values);

            if (value == null)
            {
                return ValidationResult.Error($"{_fieldModel.DisplayName} input is null");
            }

            return ValidationResult.Ok();
        }
    }
}
