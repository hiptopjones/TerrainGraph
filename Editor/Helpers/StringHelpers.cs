using System.Text.RegularExpressions;

namespace Indiecat.TerrainGraph.Editor
{
    public static class StringHelpers
    {
        public static string TitleCaseToWords(string input)
        {
            // Uses negative lookbehind to avoid matching the first word
            return Regex.Replace(input, @"(?<!^)([A-Z])", " $1");
        }
    }
}
