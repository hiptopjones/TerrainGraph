namespace CodeFirst.TerrainGraph.Editor
{
    public readonly struct ValidationResult
    {
        public readonly bool IsValid;
        public readonly string Message;

        private ValidationResult(bool isValid, string message)
        {
            IsValid = isValid;
            Message = message ?? string.Empty;
        }

        public static ValidationResult Ok()
        {
            return new ValidationResult(isValid: true, string.Empty);
        }

        public static ValidationResult Error(string message)
        {
            return new ValidationResult(isValid: false, message);
        }

        public static ValidationResult Warning(string message)
        {
            return new ValidationResult(isValid: false, message);
        }
    }
}
