using UnityEditor;

namespace CodeFirst.TerrainGraph.Editor
{
    public static class StringHelpers
    {
        public static string TitleCaseToWords(string input)
        {
            return ObjectNames.NicifyVariableName(input);
        }
    }
}
