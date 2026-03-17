namespace CodeFirst.TerrainGraph.Editor
{
    public static class ValidationHelpers
    {
        public static ValidationResult ValidateGridSizesMatch(HeightGrid grid1, HeightGrid grid2, string displayName1, string displayName2)
        {
            if (grid1 != null && grid1.IsValid &&
                grid2 != null && grid2.IsValid)
            {
                // Only run this check if both inputs are present
                // Base node validation would normally catch this, but if it only fails on one
                // of them we'd might still get here.
                if (grid1.RenderTexture.width != grid2.RenderTexture.width ||
                    grid1.RenderTexture.height != grid2.RenderTexture.height)
                {
                    return ValidationResult.Error($"{displayName1} and {displayName2} size mismatch");
                }
            }

            return ValidationResult.Ok();
        }
    }
}
