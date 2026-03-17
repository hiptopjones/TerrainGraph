using System;

namespace CodeFirst.TerrainGraph.Editor
{
    public class ValidIfRule : IValidationRule
    {
        private readonly Func<object, object, ValidationResult> _validationDelegate;

        public ValidIfRule(Func<object, object, ValidationResult> validationDelegate)
        {
            _validationDelegate = validationDelegate;
        }

        public ValidationResult Validate(object node, object values)
        {
            return _validationDelegate(node, values);
        }
    }
}
