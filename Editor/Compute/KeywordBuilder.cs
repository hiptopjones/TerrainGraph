using System.Collections.Generic;

namespace Indiecat.TerrainGraph.Editor
{
    public class KeywordBuilder
    {
        private List<string> _keywords = new List<string>();

        public string[] GetKeywords()
        {
            return _keywords.ToArray();
        }

        public void AddKeyword(string keyword)
        {
            _keywords.Add(keyword);
        }
    }
}