namespace Indiecat.TerrainGraph.Editor
{
    public interface IValidationRule
    {
        ValidationResult Validate(object node, object values);
    }
}